namespace Pure.DI.Core;

internal interface IGeneratorDiagnostic
{
    public void ReportDiagnostic(Diagnostic diagnostic);
}