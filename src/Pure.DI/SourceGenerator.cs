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
            foreach (var apiSource in _generator.GetApi())
            {
                initializationContext.AddSource(apiSource.HintName, apiSource.SourceText);
            }
        });

        var metadata = context.MetadataReferencesProvider.Collect();
        var analyzer = context.AnalyzerConfigOptionsProvider;
        var parseOptions = context.ParseOptionsProvider;
        var syntax = context.SyntaxProvider.CreateSyntaxProvider(
            static (_, _) => true,
            static (syntaxContext, _) => syntaxContext).Collect();
        
        var valuesProvider = metadata
            .Combine(analyzer)
            .Combine(parseOptions)
            .Combine(syntax);

        context.RegisterSourceOutput(valuesProvider, (sourceProductionContext, options) =>
            _generator.Generate(
                // options
                options.Left.Right,
                // metadata
                options.Left.Left.Left,
                // analyzer
                options.Left.Left.Right,
                // context
                sourceProductionContext,
                // syntax
                options.Right,
                sourceProductionContext.CancellationToken));
    }
}