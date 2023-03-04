// ReSharper disable UnusedType.Global
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ObjectAllocation.Possible
namespace Pure.DI;

[Generator(LanguageNames.CSharp)]
public class SourceGenerator: IIncrementalGenerator
{
    private readonly object _lockObject = new();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // ReSharper disable once InvocationIsSkipped
        DebugHelper.Debug();

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
                static (syntaxNode, _) => syntaxNode is MemberDeclarationSyntax or TypeDeclarationSyntax or ArgumentSyntax, 
                static (syntaxContext, _) => syntaxContext).Collect());

        context.RegisterSourceOutput(valuesProvider, (sourceProductionContext, options) =>
        {
            var changes = options.Right;
            if (changes.Length == 0)
            {
                return;
            }
            
            var parseOptions = options.Left.Right;
            var analyzerConfigOptionsProvider = options.Left.Left;
            var updates = changes.Select(change => new SyntaxUpdate(change.Node, change.SemanticModel));
            var ctx = new ContextInitializer(sourceProductionContext, parseOptions, analyzerConfigOptionsProvider);
            lock (_lockObject)
            {
                Facade
                    .GetGenerator(ctx, ctx, ctx)
                    .Generate(updates, sourceProductionContext.CancellationToken);
            }
        });
    }
}