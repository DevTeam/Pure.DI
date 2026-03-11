// ReSharper disable InvocationIsSkipped

namespace Pure.DI;

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

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

        context.RegisterPostInitializationOutput(initializationContext => {
            foreach (var apiSource in generator.Api)
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
    }
}
