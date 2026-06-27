---
name: JToken in OData v9
description: >
  Use when ingesting Newtonsoft JToken/JObject documents (e.g. from Cosmos DB or
  MS Graph) into an ASP.NET Core OData v9 API, modeling arbitrary or deeply
  nested JSON, or fixing OData "maximum nesting" / "$expand path is too deep" /
  MaxExpansionDepth errors caused by nested model types being inferred as
  entities instead of complex types.
author: jadeoti
version: 1.0.0
tags:
  - odata
  - newtonsoft
  - jtoken
  - aspnetcore
  - cosmosdb
  - msgraph
---

# Handling JToken in OData v9

How to serve arbitrary / deeply nested JSON (sourced as Newtonsoft `JToken`)
through ASP.NET Core OData 9, without losing structure and without hitting
nesting limits.

## The single most important idea

> **Nested model types are inferred as `EntityType`s (with navigation
> properties) whenever the convention builder finds a key — typically an `Id`
> property. Navigation is bounded by `$expand` and `MaxExpansionDepth`
> (default = 2), so anything nested deeper than 2 levels silently drops or errors
> with "The $expand path is too deep". Register those nested types as
> `ComplexType` so they serialize INLINE at arbitrary depth.**

`MaxExpansionDepth` only governs **entity navigation**. **Complex types do not
count toward it at all** — so modeling nested structures as complex (or open)
types is what makes unlimited nesting work.

```csharp
static IEdmModel GetEdmModel()
{
    var builder = new ODataConventionModelBuilder();
    builder.EnableLowerCamelCase();

    // Register the nested tree as (open) COMPLEX types so they serialize inline,
    // instead of being inferred as entities with navigation properties.
    builder.ComplexType<OpenObject>();
    builder.ComplexType<AdaptiveCard>();
    builder.ComplexType<AdaptiveCardElement>();   // <-- has an `Id`; would become an EntityType otherwise

    builder.EntitySet<Symptom>("Symptoms").EntityType.HasKey(s => s.Id);
    return builder.GetEdmModel();
}
```

If you skip `ComplexType<AdaptiveCardElement>()`, the `Id` property makes it an
`EntityType`, `body`/`card` become `NavigationProperty`, and nested elements
beyond depth 2 require `$expand` and break. Registering it as a complex type is
the fix.

## The maximum-nesting issue: causes and fixes

| Symptom | Cause | Fix |
|---|---|---|
| `The $expand path is too deep. The maximum depth allowed is 2.` | Nested type became an `EntityType` (had a key/`Id`); deep data needs `$expand`. | Register it as `builder.ComplexType<T>()`. |
| Nested data missing unless you add `$expand=...` | Same entity-inference problem; navigation isn't serialized inline. | Make it a complex type (inline), or model nesting via open-type `AdditionalData`. |
| Still capped after making types complex, but you DO want entity navigation | `MaxExpansionDepth` default is 2. | `.AddOData(o => o.Select().Expand().Filter().OrderBy().SetMaxExpansionDepth(n))` and/or `[EnableQuery(MaxExpansionDepth = n)]` (use a high n; `0` = unlimited). |
| Extremely deep documents (100+ levels) | OData reader/writer `MessageQuotas.MaxNestingDepth` (default 100). | Raise it: `o.MessageQuotas.MaxNestingDepth = 1000` (rarely needed). |

**Decision rule:** if the nesting is *part of one document* (a tree), it should
be **complex/open types**, never entities. Reserve entities (and `$expand`) for
genuinely independent, separately-addressable resources.

## Modeling rules for JToken-sourced data

1. **Never expose a `JToken`/`JObject` (or `JsonNode`) as a declared property
   type.** OData has no EDM type for it and serializes it as garbage:
   `"inputCard": { "@odata.type": "#Newtonsoft.Json.Linq.JObject" }` — the real
   content is dropped. The same happens with `System.Text.Json.Nodes.JsonNode`.

2. **Model open/arbitrary JSON as OPEN TYPES, not `Edm.Untyped`.** A type becomes
   an OData open type when it has an `IDictionary<string, object?> AdditionalData`
   property. This matches the **MS Graph SDK**, which supports open types but
   **not** `Edm.Untyped`.

3. **Declare the stable envelope, overflow the rest.** Declare only the
   well-known, queryable fields (so `$select`/`$filter`/`$orderby` work); route
   everything else into `AdditionalData`.

4. **Normalize nested JToken into OData-serializable CLR values**: objects →
   open-type instances (`OpenObject`), arrays → `List<object?>`, scalars → CLR
   primitives. A raw `JToken` left in `AdditionalData` fails to serialize (rule 1).

## Implementation recipe

### 1. Open-type contract + generic open object

```csharp
public interface IOpenType
{
    IDictionary<string, object?> AdditionalData { get; }
}

// Generic open complex type = arbitrary JSON object
public class OpenObject : IOpenType
{
    public IDictionary<string, object?> AdditionalData { get; init; } = new Dictionary<string, object?>();
}
```

### 2. Newtonsoft converter: JToken -> open type (the ingestion bridge)

```csharp
public class OpenTypeConverter : JsonConverter
{
    public override bool CanConvert(Type t) => typeof(IOpenType).IsAssignableFrom(t);
    public override bool CanWrite => false; // OData owns serialization; read-only

    public override object? ReadJson(JsonReader reader, Type objectType, object? existing, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;
        var source = JObject.Load(reader);
        var target = (IOpenType)Activator.CreateInstance(objectType)!;
        var declared = DeclaredMap(objectType); // name -> PropertyInfo (by [JsonProperty])

        foreach (var p in source.Properties())
        {
            if (declared.TryGetValue(p.Name, out var clr))
                clr.SetValue(target, p.Value.ToObject(clr.PropertyType, serializer)); // recurses
            else
                target.AdditionalData[p.Name] = Normalize(p.Value);
        }
        return target;
    }

    // JToken -> OData-serializable CLR value (THIS is what avoids rule-1 garbage)
    static object? Normalize(JToken t) => t.Type switch
    {
        JTokenType.Object  => new OpenObject { AdditionalData = ((JObject)t).Properties()
                                  .ToDictionary(x => x.Name, x => Normalize(x.Value)) },
        JTokenType.Array   => ((JArray)t).Select(Normalize).ToList(),
        JTokenType.Integer => t.Value<long>(),
        JTokenType.Float   => t.Value<double>(),
        JTokenType.Boolean => t.Value<bool>(),
        JTokenType.Null or JTokenType.Undefined => null,
        _ => t.Value<string>(),
    };

    public override void WriteJson(JsonWriter w, object? v, JsonSerializer s) => throw new NotSupportedException();
    // DeclaredMap: cache PropertyInfo by [JsonProperty(...)] name, skipping "AdditionalData".
}
```

### 3. Map the raw JToken to your model

```csharp
private static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
{
    Converters = { new OpenTypeConverter() },
    DateParseHandling = DateParseHandling.None, // keep raw string values; don't coerce date-like strings
});

public static Symptom ToSymptom(JToken document) => document.ToObject<Symptom>(Serializer)!;
```

When loading JToken yourself, also disable date parsing on the reader:

```csharp
using var jr = new JsonTextReader(streamReader) { DateParseHandling = DateParseHandling.None };
var root = JToken.ReadFrom(jr);
```

### 4. EDM model — see "The single most important idea" above.

## Pipeline shape

```
JToken (Cosmos/Graph)  ->  OpenTypeConverter (Newtonsoft)  ->  CLR open-type graph  ->  OData writer (Microsoft.OData.Core)
                           declared props + AdditionalData       (primitives/OpenObject/List)
```

**Key insight:** OData reflects over the **CLR object graph** via the EDM model —
it never sees your JSON or your JSON library. So the ingestion stack (Newtonsoft)
and the OData writer are fully decoupled; only the final CLR shape matters.

## Gotchas checklist

- [ ] Nested in-document types registered as `ComplexType<T>()` (not entities).
- [ ] No `JToken` / `JObject` / `JsonNode` as a declared property type.
- [ ] `AdditionalData` values are primitives / `OpenObject` / `List`, never raw `JToken`.
- [ ] `DateParseHandling.None` so date-like strings aren't mutated.
- [ ] `EnableLowerCamelCase()` if you want output keys to match camelCase source.
- [ ] Open-type dynamic values carry an `@odata.type` annotation (e.g.
      `"fileSize@odata.type": "#Int64"`) — this is expected, and matches Graph.
- [ ] Declared nullable props (e.g. `id`) emit `"id": null` when absent; drop them
      from the declared set if you need byte-for-byte fidelity.

## Verify nesting is preserved

Fetch each document, strip `@`-annotations recursively, and deep-compare depth to
the source. Depth(output) must equal Depth(source) for every record:

```python
def depth(o):
    if isinstance(o, dict):  return 1 + max([depth(v) for v in o.values()] + [0])
    if isinstance(o, list):  return 1 + max([depth(v) for v in o] + [0])
    return 0
# assert depth(stripped_output) == depth(source_jtoken)
```
