using AdaptiveCardExperiment.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AdaptiveCardExperiment.Data;

/// <summary>
/// In-memory backing store for the OData endpoint.
///
/// The source documents are obtained as raw Newtonsoft <see cref="JToken"/>s
/// (here loaded from docs/symptoms.json to stand in for the live JToken feed,
/// e.g. Cosmos DB) and each is mapped through <see cref="SymptomMapper"/> into
/// the OData-compatible <see cref="Symptom"/> model.
/// </summary>
public class SymptomStore
{
    private readonly IReadOnlyList<Symptom> _symptoms;

    public SymptomStore(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "docs", "symptoms.json");

        // Stand-in for the real source: obtain the raw JToken documents. In
        // production these arrive from the live JToken feed, not from a file.
        var documents = LoadDocuments(path);

        _symptoms = documents
            .Select(doc => SymptomMapper.ToSymptom(new Data { Value = doc }))
            .ToList();
    }

    private static IEnumerable<JToken> LoadDocuments(string path)
    {
        using var streamReader = new StreamReader(path);
        using var jsonReader = new JsonTextReader(streamReader)
        {
            DateParseHandling = DateParseHandling.None,
        };

        return JToken.ReadFrom(jsonReader) switch
        {
            JArray array => array.Children().ToList(),
            JObject single => new List<JToken> { single },
            _ => new List<JToken>(),
        };
    }

    /// <summary>Queryable source for OData ($filter/$select/$orderby/$top...).</summary>
    public IQueryable<Symptom> Query() => _symptoms.AsQueryable();

    public Symptom? Find(string id) =>
        _symptoms.FirstOrDefault(s => s.Id == id);
}
