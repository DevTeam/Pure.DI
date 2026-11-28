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

    // Roslyn runs this for every annotated class after each change of the compilation,
    // and compares the result with the previous one, so the sources are added again only when they change.
    public InterfaceGenerationOutput Build(GeneratorSyntaxContext syntaxContext, CancellationToken cancellationToken)
    {
        if (syntaxContext.Node is not ClassDeclarationSyntax classSyntax
            || syntaxContext.SemanticModel.GetDeclaredSymbol(classSyntax, cancellationToken) is not INamedTypeSymbol typeSymbol)
        {
            return InterfaceGenerationOutput.Empty;
        }

        var generatedInterfacesResult = interfaceBuilder.BuildInterfacesFor(syntaxContext.SemanticModel, typeSymbol, classSyntax);
        var warnings = generatedInterfacesResult.Warnings
            .Select(warning => new InterfaceGenerationWarning(warning.Id, warning.Message, warning.MessageKey, warning.Location))
            .ToImmutableArray();

        var sources = ImmutableArray.CreateBuilder<InterfaceGenerationSource>();
        // An empty or default list is iterated as an empty one, so the loop below is the only place that adds sources.
        var generatedSources = generatedInterfacesResult.Sources.IsDefault
            ? ImmutableArray<GeneratedInterfaceSource>.Empty
            : generatedInterfacesResult.Sources;
        foreach (var generatedInterface in generatedSources)
        {
            if (generatedInterface.Code.Count == 0)
            {
                continue;
            }

            using var rent = generatedInterface.Code.SaveToArray(Encoding.UTF8, out var buffer, out var size);
            sources.Add(new InterfaceGenerationSource(
                GetHintName(typeSymbol, generatedInterface.NamespaceName, generatedInterface.InterfaceName),
                Encoding.UTF8.GetString(buffer, 0, size)));
        }

        return sources.Count == 0 && warnings.IsEmpty
            ? InterfaceGenerationOutput.Empty
            : new InterfaceGenerationOutput(sources.ToImmutable(), warnings);
    }

    public void Generate(SourceProductionContext context, InterfaceGenerationOutput output)
    {
        foreach (var warning in output.Warnings)
        {
            ReportWarning(context, warning);
        }

        foreach (var source in output.Sources)
        {
            context.AddSource(source.HintName, SourceText.From(source.Code, Encoding.UTF8));
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

    private static void ReportWarning(SourceProductionContext context, InterfaceGenerationWarning warning)
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
