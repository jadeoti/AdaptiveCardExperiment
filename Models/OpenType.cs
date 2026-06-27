namespace AdaptiveCardExperiment.Models;

/// <summary>
/// Marker for OData/MS Graph "open types": a type with a fixed set of declared
/// properties plus an <see cref="AdditionalData"/> bag that carries any extra
/// (undeclared) properties. This mirrors the Microsoft Graph SDK pattern where
/// every model exposes an <c>AdditionalData</c> dictionary, and is used instead
/// of Edm.Untyped (which the Graph SDK does not support).
/// </summary>
public interface IOpenType
{
    IDictionary<string, object?> AdditionalData { get; }
}

/// <summary>
/// Generic open complex type representing an arbitrary JSON object whose shape
/// is not known ahead of time. All members flow through <see cref="AdditionalData"/>.
/// Nested objects become <see cref="OpenObject"/> instances and arrays become
/// <see cref="List{Object}"/>, so the whole tree is OData-serializable.
/// </summary>
public class OpenObject : IOpenType
{
    public IDictionary<string, object?> AdditionalData { get; init; }
        = new Dictionary<string, object?>();
}
