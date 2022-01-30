/*
#if ROSLYN40
namespace Pure.DI;

using Core;
using IoC;

[Generator(LanguageNames.CSharp)]
public class SourceGenerator : IIncrementalGenerator
{
    private static readonly IContainer GeneratorContainer = ContainerExtensions.Create();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxFilter = GeneratorContainer.Resolve<ISyntaxFilter>(Tags.Syntax.AsTag());
        var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider((node, _) =>
            {
                var result = syntaxFilter.Accept(node);
                if (result)
                {
                    ContainerExtensions.Log($"Accept {node} - {result}");
                }

                return result;
            }, (syntaxContext, _) => syntaxContext)
            .Where(i => syntaxFilter.Accept(i.Node))
            .Select((syntaxContext, token) => syntaxContext.SemanticModel.Compilation)
            .Collect();
        
        context.RegisterImplementationSourceOutput(syntaxProvider, (productionContext, compilation) =>
        {
            GeneratorContainer.Create().Resolve<IGenerator>().Generate(new ExecutionContext(productionContext, compilation.Last()));
        });
    }

    private class ExecutionContext : IExecutionContext
    {
        private readonly SourceProductionContext _productionContext;

        public ExecutionContext(SourceProductionContext productionContext, Compilation compilation)
        {
            _productionContext = productionContext;
            Compilation = compilation;
        }

        public Compilation Compilation { get; }

        public CancellationToken CancellationToken => _productionContext.CancellationToken;

        public void AddSource(string hintName, SourceText sourceText) =>
            _productionContext.AddSource(hintName, sourceText);

        public void ReportDiagnostic(Diagnostic diagnostic) =>
            _productionContext.ReportDiagnostic(diagnostic);
    }
}
#endif
*/