namespace Pure.DI.Core;

sealed class GeneratorDiagnostic(SourceProductionContext sourceProductionContext)
    : IGeneratorDiagnostic
{
    public void ReportDiagnostic(Diagnostic diagnostic)
    {
        try
        {
            sourceProductionContext.ReportDiagnostic(diagnostic);
        }
        catch
        {
            //
        }
    }
}