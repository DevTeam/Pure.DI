namespace Pure.DI.Core;

internal sealed class ProductionContextToSourcesAdapter(SourceProductionContext sourceProductionContext)
    : ISources
{
    public void AddSource(string hintName, SourceText sourceText) =>
        sourceProductionContext.AddSource(hintName, sourceText);
}