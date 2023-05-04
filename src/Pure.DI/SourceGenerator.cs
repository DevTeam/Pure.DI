// ReSharper disable InvocationIsSkipped
namespace Pure.DI;

[Generator(LanguageNames.CSharp)]
public class SourceGenerator: IIncrementalGenerator
{
    private static readonly object LockObject = new();
    private static CancellationTokenSource? _cancellationTokenSource;
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // ReSharper disable once InvocationIsSkipped
        // Run Rider as administrator
        DebugHelper.Debug();
        DebugHelper.Trace();

        context.RegisterPostInitializationOutput(initializationContext =>
        {
            foreach (var apiSource in Facade.GetApi(initializationContext.CancellationToken))
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
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(sourceProductionContext.CancellationToken);
            lock (LockObject)
            {
                if (_cancellationTokenSource is not null)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = cancellationTokenSource;
                }
            }

            var changes = options.Right;
            if (changes.Length == 0)
            {
                return;
            }

            var parseOptions = options.Left.Right;
            var analyzerConfigOptionsProvider = options.Left.Left;
            var updates = changes.Select(change => new SyntaxUpdate(change.Node, change.SemanticModel));
            var ctx = new ContextInitializer(sourceProductionContext, parseOptions, analyzerConfigOptionsProvider);
            var facade = Facade.Create(ctx, ctx, ctx);
            facade.Generator.Generate(updates,cancellationTokenSource.Token);
        });
    }
}