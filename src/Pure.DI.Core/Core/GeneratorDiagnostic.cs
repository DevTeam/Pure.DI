namespace Pure.DI.Core;

sealed class GeneratorDiagnostic(SourceProductionContext sourceProductionContext)
    : IGeneratorDiagnostic
{
    public void ReportDiagnostic(Diagnostic diagnostic) =>
        sourceProductionContext.ReportDiagnostic(diagnostic);
}