using Newtonsoft.Json;

namespace AdaptiveCardExperiment.Models;

/// <summary>
/// A Diagnose &amp; Solve "symptom" record (source: docs/symptoms.json).
///
/// Modelled as an OData/MS Graph <b>open type</b>: well-known fields are declared
/// (so OData can $select/$filter/$orderby over them) and any extra properties land
/// in <see cref="AdditionalData"/>. The free-form Adaptive Card tree
/// (<see cref="InputCard"/>) is likewise modelled with open types rather than
/// Edm.Untyped, because the Graph SDK supports open types but not untyped.
/// </summary>
public class Symptom : IOpenType
{
    [JsonProperty("id")]
    public string Id { get; set; } = default!;

    [JsonProperty("name")]
    public string Name { get; set; } = default!;

    [JsonProperty("shortDescription")]
    public string? ShortDescription { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>One of: alert, diagnose, wizard, workflow.</summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("guidedTroubleshooter")]
    public bool GuidedTroubleshooter { get; set; }

    [JsonProperty("diagnosticScenario")]
    public string? DiagnosticScenario { get; set; }

    [JsonProperty("subSymptomIds")]
    public List<string>? SubSymptomIds { get; set; }

    [JsonProperty("dataCollectors")]
    public List<string>? DataCollectors { get; set; }

    [JsonProperty("analyzers")]
    public List<string>? Analyzers { get; set; }

    [JsonProperty("rbac")]
    public Rbac? Rbac { get; set; }

    [JsonProperty("workflowId")]
    public string? WorkflowId { get; set; }

    [JsonProperty("canDiagnose")]
    public bool CanDiagnose { get; set; }

    [JsonProperty("recommendedSolution")]
    public RecommendedSolution? RecommendedSolution { get; set; }

    /// <summary>
    /// Proprietary, recursive Adaptive Card definition, modelled as an open type
    /// so the full nested structure is preserved without a brittle fixed schema.
    /// </summary>
    [JsonProperty("inputCard")]
    public AdaptiveCard? InputCard { get; set; }

    [JsonProperty("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonProperty("inputType")]
    public int InputType { get; set; }

    [JsonProperty("alertMetadata")]
    public AlertMetadata? AlertMetadata { get; set; }

    [JsonProperty("configuration")]
    public List<OpenObject>? Configuration { get; set; }

    [JsonProperty("items")]
    public List<OpenObject>? Items { get; set; }

    [JsonProperty("base64EncodedInstruction")]
    public string? Base64EncodedInstruction { get; set; }

    [JsonProperty("partitionKey")]
    public string? PartitionKey { get; set; }

    [JsonProperty("_etag")]
    public string? ETag { get; set; }

    [JsonProperty("_ts")]
    public long Timestamp { get; set; }

    /// <summary>Overflow bag for any undeclared symptom properties (open type).</summary>
    [JsonIgnore]
    public IDictionary<string, object?> AdditionalData { get; init; }
        = new Dictionary<string, object?>();
}

/// <summary>
/// Root of an Adaptive Card, modelled as an open type. Only the stable envelope
/// (<see cref="Type"/>, <see cref="Version"/>, <see cref="Body"/>) is declared;
/// any other card-level properties flow into <see cref="AdditionalData"/>.
/// </summary>
public class AdaptiveCard : IOpenType
{
    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("version")]
    public string? Version { get; set; }

    [JsonProperty("body")]
    public List<AdaptiveCardElement>? Body { get; set; }

    [JsonIgnore]
    public IDictionary<string, object?> AdditionalData { get; init; }
        = new Dictionary<string, object?>();
}

/// <summary>
/// A single Adaptive Card element (e.g. Input.Text, Input.Radio, Input.File),
/// modelled as an open type. The element <see cref="Type"/> and <see cref="Id"/>
/// are declared; all other element-specific properties (label, placeholder,
/// choices, options, validationProperties, and nested <c>card</c> sub-elements)
/// flow into <see cref="AdditionalData"/>, so the tree recurses to any depth.
/// </summary>
public class AdaptiveCardElement : IOpenType
{
    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonIgnore]
    public IDictionary<string, object?> AdditionalData { get; init; }
        = new Dictionary<string, object?>();
}

/// <summary>Role-based access control gate for a symptom (open type).</summary>
public class Rbac : IOpenType
{
    [JsonProperty("allowAnyUsers")]
    public bool AllowAnyUsers { get; set; }

    [JsonProperty("applicationRoles")]
    public List<string>? ApplicationRoles { get; set; }

    [JsonIgnore]
    public IDictionary<string, object?> AdditionalData { get; init; }
        = new Dictionary<string, object?>();
}

/// <summary>Metadata driving the alert banner for "alert" type symptoms (open type).</summary>
public class AlertMetadata : IOpenType
{
    [JsonProperty("version")]
    public string? Version { get; set; }

    [JsonProperty("url")]
    public string? Url { get; set; }

    [JsonProperty("limit")]
    public int? Limit { get; set; }

    [JsonProperty("filter")]
    public string? Filter { get; set; }

    [JsonProperty("lookBackPeriodInDays")]
    public int? LookBackPeriodInDays { get; set; }

    [JsonProperty("selectFilter")]
    public string? SelectFilter { get; set; }

    [JsonProperty("hideIfNoAlert")]
    public bool? HideIfNoAlert { get; set; }

    [JsonProperty("viewGuidanceUrl")]
    public string? ViewGuidanceUrl { get; set; }

    [JsonIgnore]
    public IDictionary<string, object?> AdditionalData { get; init; }
        = new Dictionary<string, object?>();
}

/// <summary>A recommended help article / rich solution for a symptom (open type).</summary>
public class RecommendedSolution : IOpenType
{
    [JsonProperty("articleId")]
    public string? ArticleId { get; set; }

    [JsonProperty("articleContent")]
    public string? ArticleContent { get; set; }

    /// <summary>Open key/value replacement map.</summary>
    [JsonProperty("articleReplaceMap")]
    public OpenObject? ArticleReplaceMap { get; set; }

    [JsonProperty("productId")]
    public string? ProductId { get; set; }

    [JsonProperty("productName")]
    public string? ProductName { get; set; }

    [JsonProperty("useRichContent")]
    public bool? UseRichContent { get; set; }

    /// <summary>Open rich-content payload (Apollo/HTML).</summary>
    [JsonProperty("richContent")]
    public OpenObject? RichContent { get; set; }

    [JsonIgnore]
    public IDictionary<string, object?> AdditionalData { get; init; }
        = new Dictionary<string, object?>();
}
