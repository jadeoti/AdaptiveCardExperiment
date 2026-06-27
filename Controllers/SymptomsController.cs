using AdaptiveCardExperiment.Data;
using AdaptiveCardExperiment.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AdaptiveCardExperiment.Controllers;

/// <summary>
/// OData v9 endpoint for the symptoms collection.
/// Route base is configured in Program.cs (e.g. /odata/Symptoms).
/// </summary>
public class SymptomsController : ODataController
{
    private readonly SymptomStore _store;

    public SymptomsController(SymptomStore store) => _store = store;

    // GET /odata/Symptoms  (+ $select/$filter/$orderby/$top/$skip/$count)
    [EnableQuery(PageSize = 100, MaxExpansionDepth = 5)]
    public IQueryable<Symptom> Get() => _store.Query();

    // GET /odata/Symptoms('a.symptom.id')
    [EnableQuery]
    public ActionResult<Symptom> Get([FromRoute] string key)
    {
        var symptom = _store.Find(key);
        return symptom is null ? NotFound() : Ok(symptom);
    }
}
