namespace Pure.DI.InterfaceGeneration;

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Pure.DI;
using static LogMetadata;

sealed class InterfaceGenerator(IInterfaceBuilder interfaceBuilder) : IInterfaceGenerator
{
    public bool HasGenerateInterfaceAttribute(ClassDeclarationSyntax classSyntax) => classSyntax.AttributeLists
        .SelectMany(list => list.Attributes)
        .Concat(
            classSyntax.Members
                .SelectMany(member => member.AttributeLists)
                .SelectMany(list => list.Attributes))
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

            var generatedInterfacesResult = interfaceBuilder.BuildInterfacesFor(syntaxContext.SemanticModel, typeSymbol, classSyntax);
            foreach (var warning in generatedInterfacesResult.Warnings)
            {
                ReportWarning(context, warning);
            }

            var generatedInterfaces = generatedInterfacesResult.Sources;
            if (generatedInterfaces.IsDefaultOrEmpty)
            {
                continue;
            }

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var generatedInterface in generatedInterfaces)
            {
                if (generatedInterface.Code.Count == 0)
                {
                    continue;
                }

                using var rent = generatedInterface.Code.SaveToArray(Encoding.UTF8, out var buffer, out var size);
                context.AddSource(
                    GetHintName(typeSymbol, generatedInterface.NamespaceName, generatedInterface.InterfaceName),
                    SourceText.From(buffer, size, Encoding.UTF8, SourceHashAlgorithm.Sha1, false, true));
            }
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

        return string.Equals(shortName, Names.GenerateInterfaceAttributeName, StringComparison.Ordinal)
               || string.Equals(shortName, $"{Names.GenerateInterfaceAttributeName}Attribute", StringComparison.Ordinal);
    }

    private static string GetHintName(INamedTypeSymbol typeSymbol, string namespaceName, string interfaceName)
    {
        var fullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", string.Empty)
            .Replace('<', '{')
            .Replace('>', '}')
            .Replace(',', '_')
            .Replace(' ', '_')
            .Replace(':', '_');

        var ns = SanitizeFileNamePart(namespaceName);
        var iface = SanitizeFileNamePart(interfaceName);
        return $"{fullName}.{ns}.{iface}.Interface.g.cs";
    }

    private static string SanitizeFileNamePart(string value) => string.IsNullOrWhiteSpace(value)
        ? "Global"
        : value
            .Replace("global::", string.Empty)
            .Replace('<', '{')
            .Replace('>', '}')
            .Replace(',', '_')
            .Replace(' ', '_')
            .Replace(':', '_');

    private static void ReportWarning(SourceProductionContext context, GeneratedInterfaceWarning warning)
    {
        var descriptor = new DiagnosticDescriptor(
            warning.Id,
            "WRN",
            warning.Message,
            GetCategory(warning.Id),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: GetDescription(warning.Id),
            helpLinkUri: GetHelpLink(warning.Id));
        var properties = ImmutableDictionary<string, string?>.Empty.Add("puredi.messageKey", warning.MessageKey);
        context.ReportDiagnostic(Diagnostic.Create(descriptor, warning.Location, properties));
    }
}
