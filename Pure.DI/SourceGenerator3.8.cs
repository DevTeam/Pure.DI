namespace Pure.DI;

using Core;
using IoC;

[Generator]
public class SourceGenerator : ISourceGenerator
{
    private static readonly IContainer GeneratorContainer = ContainerExtensions.Create();

    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context)
    {
        using var container = GeneratorContainer.Create();
        container.Resolve<IGenerator>().Generate(new ExecutionContext(context));
    }

    private class ExecutionContext : IExecutionContext
    {
        private readonly GeneratorExecutionContext _context;

        public ExecutionContext(GeneratorExecutionContext context) => _context = context;

        public Compilation Compilation => _context.Compilation;

        public CancellationToken CancellationToken => _context.CancellationToken;

        public void AddSource(string hintName, SourceText sourceText)
        {
            Action<string, SourceText> addSource = _context.AddSource;
            addSource(hintName, sourceText);
        }

        public void ReportDiagnostic(Diagnostic diagnostic) =>
            _context.ReportDiagnostic(diagnostic);

        public bool TryGetOption(string optionName, out string value) => 
            _context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(optionName, out value!);
    }
}