using AdaptiveCardExperiment.Models;
using Newtonsoft.Json;

namespace AdaptiveCardExperiment.Data;

/// <summary>
/// Maps a raw <see cref="Data"/> (JToken) document onto the OData-compatible
/// <see cref="Symptom"/> model. The <see cref="OpenTypeConverter"/> handles the
/// open-type split (declared properties vs. <c>AdditionalData</c> overflow) and
/// normalises nested content so the result is OData-serializable.
/// </summary>
public static class SymptomMapper
{
    private static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
    {
        Converters = { new OpenTypeConverter() },
        // Preserve raw string values; don't let Newtonsoft coerce date-like strings.
        DateParseHandling = DateParseHandling.None,
    });

    public static Symptom ToSymptom(Data data) =>
        data.Value.ToObject<Symptom>(Serializer)
        ?? throw new JsonSerializationException("Symptom document was null.");
}
