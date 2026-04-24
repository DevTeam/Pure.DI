namespace Pure.DI.InterfaceGeneration;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

sealed class GeneratedInterfaceDetails(
    SemanticModel semanticModel,
    AttributeData? generationAttribute,
    ITypeSymbol typeSymbol,
    ClassDeclarationSyntax classSyntax)
{
    public SemanticModel SemanticModel { get; } = semanticModel;

    public string NamespaceName { get; } = PrepareNamespaceValue(generationAttribute, typeSymbol.ContainingNamespace.ToDisplayString());

    public string InterfaceName { get; } = PrepareValue(
        generationAttribute,
        Names.InterfaceParameterName,
        $"I{classSyntax.Identifier.Text}");

    public string AccessLevel { get; } = PrepareValue(
        generationAttribute,
        Names.AsInternalParameterName,
        false)
        ? "internal"
        : "public";

    public string ClassDocumentation { get; set; } = string.Empty;

    public string GenericType { get; set; } = string.Empty;

    public ImmutableArray<PropertyInfo> PropertyInfos { get; set; } = ImmutableArray<PropertyInfo>.Empty;

    public ImmutableArray<MethodInfo> MethodInfos { get; set; } = ImmutableArray<MethodInfo>.Empty;

    public ImmutableArray<EventInfo> Events { get; set; } = ImmutableArray<EventInfo>.Empty;

    private static string PrepareNamespaceValue(AttributeData? generationAttribute, string defaultValue) =>
        PrepareValue(generationAttribute, Names.NamespaceParameterName, defaultValue);

    private static T PrepareValue<T>(AttributeData? generationAttribute, string key, T defaultValue)
    {
        var parameterSymbol = generationAttribute?.AttributeConstructor?.Parameters.SingleOrDefault(x => x.Name == key);
        if (parameterSymbol == null)
        {
            return defaultValue;
        }

        var index = generationAttribute!.AttributeConstructor!.Parameters.IndexOf(parameterSymbol);
        var result = generationAttribute.ConstructorArguments[index].Value;
        if (result != null)
        {
            return (T)result;
        }

        return defaultValue;
    }
}
