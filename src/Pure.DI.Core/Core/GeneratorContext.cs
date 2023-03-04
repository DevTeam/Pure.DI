// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable HeapView.ObjectAllocation.Evident
namespace Pure.DI.Core;

internal class GeneratorContext : IContextInitializer, IContextOptions, IContextProducer, IContextDiagnostic
{
    private static readonly Exception NotInitializedException = new InvalidOperationException("The generator context is not initialized.");
    private IContextDiagnostic? _diagnostic;
    private IContextOptions? _options;
    private IContextProducer? _producer;

    public void Initialize(IContextOptions options, IContextProducer producer, IContextDiagnostic diagnostic)
    {
        _options = options;
        _producer = producer;
        _diagnostic = diagnostic;
    }

    public ParseOptions ParseOptions => _options?.ParseOptions ?? throw NotInitializedException;

    public AnalyzerConfigOptions GlobalOptions => _options?.GlobalOptions ?? throw NotInitializedException;

    public AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _options?.GetOptions(tree) ?? throw NotInitializedException;

    public void ReportDiagnostic(Diagnostic diagnostic) => (_diagnostic ?? throw NotInitializedException).ReportDiagnostic(diagnostic);
    
    public void AddSource(string hintName, SourceText sourceText) => (_producer ?? throw NotInitializedException).AddSource(hintName, sourceText);
}