namespace Pure.DI.Core;

internal class GeneratorSources(SourceProductionContext sourceProductionContext) : IGeneratorSources
{
    public void AddSource(string hintName, SourceText sourceText) =>
        sourceProductionContext.AddSource(hintName, sourceText);
}