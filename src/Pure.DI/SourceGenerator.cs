// ReSharper disable InvocationIsSkipped
namespace Pure.DI;

[Generator(LanguageNames.CSharp)]
public class SourceGenerator: IIncrementalGenerator
{
    private readonly Generator _generator = new();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // ReSharper disable once InvocationIsSkipped
        // Run Rider as administrator
        DebugHelper.Debug();
        DebugHelper.Trace();
        
        context.RegisterPostInitializationOutput(initializationContext =>
        {
            foreach (var apiSource in _generator.Api)
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
            _generator.Generate(
                options.Left.Right,
                options.Left.Left,
                sourceProductionContext,
                options.Right,
                sourceProductionContext.CancellationToken));
    }
}