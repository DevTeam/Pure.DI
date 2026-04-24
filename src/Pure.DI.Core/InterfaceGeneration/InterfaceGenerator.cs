namespace Pure.DI.InterfaceGeneration;

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Pure.DI;

sealed class InterfaceGenerator(IInterfaceBuilder interfaceBuilder) : IInterfaceGenerator
{
    public bool HasGenerateInterfaceAttribute(ClassDeclarationSyntax classSyntax) => classSyntax.AttributeLists
        .SelectMany(list => list.Attributes)
        .Any(attribute => attribute.Name.ToString().Contains(Names.GenerateInterfaceAttributeName, StringComparison.Ordinal));

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
