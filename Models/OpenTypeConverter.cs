using System.Collections.Concurrent;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AdaptiveCardExperiment.Models;

/// <summary>
/// Newtonsoft converter that materialises an <see cref="IOpenType"/> from a
/// <see cref="JToken"/>. JSON properties matching a declared CLR property (by
/// <see cref="JsonPropertyAttribute"/>) bind to it; everything else is captured in
/// <see cref="IOpenType.AdditionalData"/>, normalised into OData-friendly CLR
/// shapes (<see cref="OpenObject"/> for objects, <see cref="List{Object}"/> for
/// arrays, primitives otherwise) so the OData writer can emit it.
///
/// This is the ingestion bridge for the real pipeline, where symptoms arrive as
/// <see cref="JToken"/> documents (e.g. from Cosmos DB) rather than from a file.
/// </summary>
public class OpenTypeConverter : JsonConverter
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, PropertyInfo>> Cache = new();

    public override bool CanConvert(Type objectType) =>
        typeof(IOpenType).IsAssignableFrom(objectType);

    // OData owns serialization; this converter is read-only (ingestion only).
    public override bool CanWrite => false;

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var source = JObject.Load(reader);
        var target = (IOpenType)Activator.CreateInstance(objectType)!;
        var declared = DeclaredMap(objectType);

        foreach (var property in source.Properties())
        {
            if (declared.TryGetValue(property.Name, out var clrProperty))
                clrProperty.SetValue(target, property.Value.ToObject(clrProperty.PropertyType, serializer));
            else
                target.AdditionalData[property.Name] = Normalize(property.Value);
        }

        return target;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) =>
        throw new NotSupportedException("OpenTypeConverter is read-only; OData handles serialization.");

    /// <summary>Converts an undeclared JToken into an OData-serializable CLR value.</summary>
    private static object? Normalize(JToken token) => token.Type switch
    {
        JTokenType.Object => new OpenObject
        {
            AdditionalData = ((JObject)token).Properties()
                .ToDictionary(p => p.Name, p => Normalize(p.Value)),
        },
        JTokenType.Array => ((JArray)token).Select(Normalize).ToList(),
        JTokenType.Integer => token.Value<long>(),
        JTokenType.Float => token.Value<double>(),
        JTokenType.Boolean => token.Value<bool>(),
        JTokenType.Null or JTokenType.Undefined => null,
        _ => token.Value<string>(), // String, Guid, Uri, Date (date parsing is disabled on read)
    };

    private static IReadOnlyDictionary<string, PropertyInfo> DeclaredMap(Type type) =>
        Cache.GetOrAdd(type, static t =>
        {
            var map = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
            foreach (var pi in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (pi.Name == nameof(IOpenType.AdditionalData)) continue;
                var name = pi.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? pi.Name;
                map[name] = pi;
            }
            return map;
        });
}
