namespace Pure.DI.Core;

sealed class GeneratorDiagnostic(SourceProductionContext sourceProductionContext, ImmutableArray<SyntaxTree> setupTrees)
    : IGeneratorDiagnostic
{
    // Whether a diagnostic refers to a file without setups, see CompilationSnapshotComparer.
    public bool HasDiagnosticsOutsideSetups { get; private set; }

    public void ReportDiagnostic(Diagnostic diagnostic)
    {
        if (!HasDiagnosticsOutsideSetups && IsOutsideSetups(diagnostic))
        {
            HasDiagnosticsOutsideSetups = true;
        }

        try
        {
            sourceProductionContext.ReportDiagnostic(diagnostic);
        }
        catch
        {
            //
        }
    }

    private bool IsOutsideSetups(Diagnostic diagnostic)
    {
        if (IsOutsideSetups(diagnostic.Location))
        {
            return true;
        }

        foreach (var location in diagnostic.AdditionalLocations)
        {
            if (IsOutsideSetups(location))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsOutsideSetups(Location location) =>
        location.SourceTree is {} tree && !setupTrees.Contains(tree);
}
