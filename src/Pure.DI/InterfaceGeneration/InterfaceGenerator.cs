namespace Pure.DI;

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

[Generator]
public sealed class InterfaceGenerator
{
    public const string GenerateInterfaceAttributeName = "GenerateInterface";
    public const string IgnoreInterfaceAttributeName = "IgnoreInterface";
    public const string NamespaceParameterName = "namespaceName";
    public const string InterfaceParameterName = "interfaceName";
    public const string AsInternalParameterName = "asInternal";

    public IEnumerable<Source> Api => new Source[]
    {
        new($"{GenerateInterfaceAttributeName}.Attribute.g.cs", SourceText.From(RegisterAttributesExtensions.GenerateInterfaceAttributeSource, System.Text.Encoding.UTF8)),
        new($"{IgnoreInterfaceAttributeName}.Attribute.g.cs", SourceText.From(RegisterAttributesExtensions.IgnoreInterfaceAttributeSource, System.Text.Encoding.UTF8)),
    };

    public void Generate(SourceProductionContext context, ImmutableArray<GeneratorSyntaxContext> syntaxContexts)
    {
        if (syntaxContexts.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var syntaxContext in syntaxContexts)
        {
            if (syntaxContext.Node is not Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax classSyntax)
            {
                continue;
            }

            if (syntaxContext.SemanticModel.GetDeclaredSymbol(classSyntax) is not INamedTypeSymbol typeSymbol)
            {
                continue;
            }

            var code = InterfaceBuilder.BuildInterfaceFor(typeSymbol, classSyntax);
            if (string.IsNullOrWhiteSpace(code))
            {
                continue;
            }

            context.AddSource(GetHintName(typeSymbol), SourceText.From(code, System.Text.Encoding.UTF8));
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
