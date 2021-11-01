//#if ROSLYN38
namespace Pure.DI
{
    using System;
    using System.Threading;
    using Core;
    using IoC;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Text;

    [Generator]
    public class SourceGenerator: ISourceGenerator
    {
        private static readonly IContainer GeneratorContainer = ContainerExtensions.Create();

        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context) =>
            GeneratorContainer.Create().Resolve<IGenerator>().Generate(new ExecutionContext(context));
        
        private class ExecutionContext: IExecutionContext
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
        }
    }
}
//#endif