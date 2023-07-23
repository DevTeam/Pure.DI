// ReSharper disable InvocationIsSkipped
namespace Pure.DI;

[Generator(LanguageNames.CSharp)]
public class SourceGenerator: IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // ReSharper disable once InvocationIsSkipped
        // Run Rider as administrator
        DebugHelper.Debug();
        DebugHelper.Trace();
        
        context.RegisterPostInitializationOutput(initializationContext =>
        {
            var composition = new CompositionBase();
            foreach (var apiSource in composition.ApiBuilder.Build(Unit.Shared, initializationContext.CancellationToken))
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
        {
            var changes = options.Right;
            if (changes.Length == 0)
            {
                return;
            }

            var updates = changes.Select(change => new SyntaxUpdate(change.Node, change.SemanticModel));
            var ctx = new Context(sourceProductionContext, options.Left.Right, options.Left.Left);
            var composition = new Composition(ctx, ctx, ctx);
            composition.Generator.Build(updates, sourceProductionContext.CancellationToken);
        });
    }
}