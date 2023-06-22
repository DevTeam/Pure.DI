﻿namespace Pure.DI;

using Core;
using IoC;

[Generator]
internal sealed class SourceGenerator : ISourceGenerator
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

        public ParseOptions ParseOptions => _context.ParseOptions;

        public void AddSource(string hintName, SourceText sourceText)
        {
            Action<string, SourceText> addSource = _context.AddSource;
            addSource(hintName, sourceText);
        }

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            var location = diagnostic.Location;
            if (location.IsInSource)
            {
                try
                {
                    // Make sure that the semantic model is available
                    Compilation.GetSemanticModel(location.SourceTree);
                }
                catch
                {
                    return;
                }
            }
            
            _context.ReportDiagnostic(diagnostic);
        }

        public bool TryGetOption(string optionName, out string value) => 
            _context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(optionName, out value!);
    }
}