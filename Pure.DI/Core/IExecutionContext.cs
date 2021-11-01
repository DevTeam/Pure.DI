namespace Pure.DI.Core
{
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Text;

    internal interface IExecutionContext
    {
        Compilation Compilation { get; }
        
        CancellationToken CancellationToken { get; }
        
        void AddSource(string hintName, SourceText sourceText);

        public void ReportDiagnostic(Diagnostic diagnostic);
    }
}