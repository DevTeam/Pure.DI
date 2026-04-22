namespace Pure.DI.InterfaceGeneration;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Pure.DI;

sealed class InterfaceGenerator : IInterfaceGenerator
{
    private readonly IInterfaceBuilder _interfaceBuilder;
    private readonly IInterfaceAttributeSources _attributeSources;

    internal InterfaceGenerator(
        IInterfaceBuilder interfaceBuilder,
        IInterfaceAttributeSources attributeSources)
    {
        _interfaceBuilder = interfaceBuilder;
        _attributeSources = attributeSources;
    }

    public IEnumerable<Source> Api =>
    [
        new($"{Names.GenerateInterfaceAttributeName}.Attribute.g.cs", SourceText.From(_attributeSources.GenerateInterfaceAttributeSource, Encoding.UTF8)),
        new($"{Names.IgnoreInterfaceAttributeName}.Attribute.g.cs", SourceText.From(_attributeSources.IgnoreInterfaceAttributeSource, Encoding.UTF8))
    ];

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

            var code = _interfaceBuilder.BuildInterfaceFor(typeSymbol, classSyntax);
            if (string.IsNullOrWhiteSpace(code))
            {
                continue;
            }

            context.AddSource(GetHintName(typeSymbol), SourceText.From(code, Encoding.UTF8));
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
