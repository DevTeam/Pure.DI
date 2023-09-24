namespace Pure.DI.Core;

internal class GeneratorSources: IGeneratorSources
{
    private readonly SourceProductionContext _sourceProductionContext;

    public GeneratorSources(SourceProductionContext sourceProductionContext) => 
        _sourceProductionContext = sourceProductionContext;

    public void AddSource(string hintName, SourceText sourceText) =>
        _sourceProductionContext.AddSource(hintName, sourceText);
}