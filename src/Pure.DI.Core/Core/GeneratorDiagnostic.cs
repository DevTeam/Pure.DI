namespace Pure.DI.Core;

internal class GeneratorDiagnostic: IGeneratorDiagnostic
{
    private readonly SourceProductionContext _sourceProductionContext;

    public GeneratorDiagnostic(SourceProductionContext sourceProductionContext) => 
        _sourceProductionContext = sourceProductionContext;

    public void ReportDiagnostic(Diagnostic diagnostic) =>
        _sourceProductionContext.ReportDiagnostic(diagnostic);
}