using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.Edm;
using AdaptiveCardExperiment.Data;
using AdaptiveCardExperiment.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SymptomStore>();

builder.Services
    .AddControllers()
    .AddOData(options => options
        .EnableQueryFeatures(maxTopValue: 100)
        .AddRouteComponents("odata", GetEdmModel()));

var app = builder.Build();

app.MapControllers();

app.Run();

static IEdmModel GetEdmModel()
{
    var builder = new ODataConventionModelBuilder();
    builder.EnableLowerCamelCase();

    // Register the Adaptive Card tree as (open) complex types so they serialize
    // inline, instead of being inferred as entities with navigation properties.
    builder.ComplexType<OpenObject>();
    builder.ComplexType<AdaptiveCard>();
    builder.ComplexType<AdaptiveCardElement>();

    builder.EntitySet<Symptom>("Symptoms").EntityType.HasKey(s => s.Id);
    return builder.GetEdmModel();
}
