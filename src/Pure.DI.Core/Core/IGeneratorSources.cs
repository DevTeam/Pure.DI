namespace Pure.DI.Core;

internal interface IGeneratorSources
{
    void AddSource(string hintName, SourceText sourceText);
}