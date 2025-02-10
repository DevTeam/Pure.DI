// ReSharper disable InvocationIsSkipped

namespace Pure.DI;

[Generator(LanguageNames.CSharp)]
public class SourceGenerator : IIncrementalGenerator
{
    private static readonly Generator Generator = new();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // ReSharper disable once InvocationIsSkipped
        // Run Rider as administrator
        DebugHelper.Debug();

        context.RegisterPostInitializationOutput(initializationContext =>
        {
            foreach (var apiSource in Generator.Api)
            {
                initializationContext.AddSource(apiSource.HintName, apiSource.SourceText);
            }
        });

        var valuesProvider = context.AnalyzerConfigOptionsProvider
            .Combine(context.ParseOptionsProvider)
            .Combine(context.SyntaxProvider.CreateSyntaxProvider(
                static (_, _) => true,
                static (syntaxContext, _) => syntaxContext).Collect());

        context.RegisterSourceOutput(valuesProvider, (sourceProductionContext, options) =>
            Generator.Generate(
                options.Left.Right,
                options.Left.Left,
                sourceProductionContext,
                options.Right,
                sourceProductionContext.CancellationToken));
    }
}