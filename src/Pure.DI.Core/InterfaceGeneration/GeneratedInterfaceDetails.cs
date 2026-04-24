namespace Pure.DI.InterfaceGeneration;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

sealed class GeneratedInterfaceDetails(
    SemanticModel semanticModel,
    string namespaceName,
    string interfaceName,
    bool asInternal)
{
    public SemanticModel SemanticModel { get; } = semanticModel;

    public string NamespaceName { get; } = namespaceName;

    public string InterfaceName { get; } = interfaceName;

    public string AccessLevel { get; } = asInternal
        ? "internal"
        : "public";

    public string ClassDocumentation { get; set; } = string.Empty;

    public string GenericType { get; set; } = string.Empty;

    public ImmutableArray<PropertyInfo> PropertyInfos { get; set; } = ImmutableArray<PropertyInfo>.Empty;

    public ImmutableArray<MethodInfo> MethodInfos { get; set; } = ImmutableArray<MethodInfo>.Empty;

    public ImmutableArray<EventInfo> Events { get; set; } = ImmutableArray<EventInfo>.Empty;
}
