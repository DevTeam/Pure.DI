namespace Pure.DI.InterfaceGeneration;

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Pure.DI;
using CoreNames = Pure.DI.Core.Names;

sealed class InterfaceGenerator(IInterfaceBuilder interfaceBuilder) : IInterfaceGenerator
{
    public bool HasGenerateInterfaceAttribute(ClassDeclarationSyntax classSyntax) => classSyntax.AttributeLists
        .SelectMany(list => list.Attributes)
        .Any(attribute => IsGenerateInterfaceAttributeName(attribute.Name.ToString()));

    public void Generate(SourceProductionContext context, ImmutableArray<GeneratorSyntaxContext> syntaxContexts)
    {
        if (syntaxContexts.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var syntaxContext in syntaxContexts)
        {
            if (syntaxContext.Node is not ClassDeclarationSyntax classSyntax)
            {
                continue;
            }

            if (syntaxContext.SemanticModel.GetDeclaredSymbol(classSyntax) is not INamedTypeSymbol typeSymbol)
            {
                continue;
            }

            var code = interfaceBuilder.BuildInterfaceFor(syntaxContext.SemanticModel, typeSymbol, classSyntax);
            if (code.Count == 0)
            {
                continue;
            }

            using var rent = code.SaveToArray(Encoding.UTF8, out var buffer, out var size);
            context.AddSource(GetHintName(typeSymbol), SourceText.From(buffer, size, Encoding.UTF8, SourceHashAlgorithm.Sha1, false, true));
        }
    }

    private static bool IsGenerateInterfaceAttributeName(string attributeName)
    {
        var shortName = attributeName;
        var namespaceSeparator = shortName.LastIndexOf('.');
        if (namespaceSeparator >= 0)
        {
            shortName = shortName[(namespaceSeparator + 1)..];
        }

        var globalNamespaceSeparator = shortName.LastIndexOf("::", StringComparison.Ordinal);
        if (globalNamespaceSeparator >= 0)
        {
            shortName = shortName[(globalNamespaceSeparator + 2)..];
        }

        return string.Equals(shortName, CoreNames.GenerateInterfaceAttributeName, StringComparison.Ordinal)
               || string.Equals(shortName, $"{CoreNames.GenerateInterfaceAttributeName}Attribute", StringComparison.Ordinal);
    }

    private static string GetHintName(INamedTypeSymbol typeSymbol)
    {
        var fullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", string.Empty)
            .Replace('<', '{')
            .Replace('>', '}')
            .Replace(',', '_')
            .Replace(' ', '_')
            .Replace(':', '_');

        return $"{fullName}.Interface.g.cs";
    }
}
