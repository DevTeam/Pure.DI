// ReSharper disable InvocationIsSkipped

namespace Pure.DI;

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Generator(LanguageNames.CSharp)]
[SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1036:Specify analyzer banned API enforcement setting")]
public class SourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // ReSharper disable once InvocationIsSkipped
        // Run Rider as administrator
        DebugHelper.DebugIfNeeded();
        var generator = new Generator();
        var interfaceGenerator = new InterfaceGenerator();

        context.RegisterPostInitializationOutput(initializationContext => {
            foreach (var apiSource in generator.Api)
            {
                initializationContext.AddSource(apiSource.HintName, apiSource.SourceText);
            }

            foreach (var apiSource in interfaceGenerator.Api)
            {
                initializationContext.AddSource(apiSource.HintName, apiSource.SourceText);
            }
        });

        var setupContexts = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (_, _) => true,
                static (syntaxContext, _) => syntaxContext)
            .Collect();

        var optionsProvider = context.AnalyzerConfigOptionsProvider
            .Combine(context.ParseOptionsProvider);

        var valuesProvider = optionsProvider
            .Combine(context.CompilationProvider)
            .Combine(setupContexts);

        context.RegisterSourceOutput(valuesProvider, (sourceProductionContext, options) =>
        {
            var ((configAndParse, _), updates) = options;
            var (config, parseOptions) = configAndParse;
            generator.Generate(
                parseOptions,
                config,
                sourceProductionContext,
                updates,
                sourceProductionContext.CancellationToken);
        });

        var interfaceUpdates = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                static (syntaxContext, _) => syntaxContext)
            .Where(static syntaxContext => HasGenerateInterfaceAttribute((ClassDeclarationSyntax)syntaxContext.Node))
            .Collect();

        context.RegisterSourceOutput(interfaceUpdates, (sourceProductionContext, updates) =>
        {
            interfaceGenerator.Generate(sourceProductionContext, updates);
        });
    }

    private static bool HasGenerateInterfaceAttribute(ClassDeclarationSyntax classSyntax) =>
        classSyntax.AttributeLists
            .SelectMany(list => list.Attributes)
            .Any(attribute => attribute.Name.ToString().Contains(InterfaceGenerator.GenerateInterfaceAttributeName));
}
