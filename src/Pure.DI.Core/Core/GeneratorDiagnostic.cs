namespace Pure.DI.Core;

internal class GeneratorDiagnostic(SourceProductionContext sourceProductionContext)
    : IGeneratorDiagnostic
{
    public void ReportDiagnostic(Diagnostic diagnostic) =>
        sourceProductionContext.ReportDiagnostic(diagnostic);
}