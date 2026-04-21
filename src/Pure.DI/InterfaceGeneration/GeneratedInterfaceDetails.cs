namespace Pure.DI;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal sealed class GeneratedInterfaceDetails(
    AttributeData? generationAttribute,
    ITypeSymbol typeSymbol,
    ClassDeclarationSyntax classSyntax)
{
    public string NamespaceName { get; } = PrepareNamespaceValue(generationAttribute, typeSymbol.ContainingNamespace.ToDisplayString());

    public string InterfaceName { get; } = PrepareValue(
        generationAttribute,
        InterfaceGenerator.InterfaceParameterName,
        $"I{classSyntax.Identifier.Text}");

    public string AccessLevel { get; } = PrepareValue(
        generationAttribute,
        InterfaceGenerator.AsInternalParameterName,
        false)
        ? "internal"
        : "public";

    private static string PrepareNamespaceValue(AttributeData? generationAttribute, string defaultValue)
    {
        var value = PrepareValue(generationAttribute, InterfaceGenerator.NamespaceParameterName, defaultValue);
        return value ?? defaultValue;
    }

    private static T PrepareValue<T>(AttributeData? generationAttribute, string key, T defaultValue)
    {
        var parameterSymbol = generationAttribute?.AttributeConstructor?.Parameters.SingleOrDefault(x => x.Name == key);
        if (parameterSymbol != null)
        {
            var index = generationAttribute!.AttributeConstructor!.Parameters.IndexOf(parameterSymbol);
            var result = generationAttribute.ConstructorArguments[index].Value;
            if (result != null)
            {
                return (T)result;
            }
        }

        return defaultValue;
    }
}
