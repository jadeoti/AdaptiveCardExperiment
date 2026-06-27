using Newtonsoft.Json.Linq;

namespace AdaptiveCardExperiment.Data;

/// <summary>
/// Raw inbound symptom document, backed by a Newtonsoft <see cref="JToken"/>.
///
/// In the real project this <see cref="Value"/> is supplied directly by the live
/// source (e.g. a Cosmos DB query, whose .NET SDK is Newtonsoft-native) rather
/// than read from a file. It is the untyped staging shape that is subsequently
/// mapped into the OData-compatible <c>Symptom</c> model.
/// </summary>
public class Data
{
    public required JToken Value { get; init; }
}
