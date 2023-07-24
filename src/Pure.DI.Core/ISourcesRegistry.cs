namespace Pure.DI;

public interface ISourcesRegistry
{
    void AddSource(string hintName, SourceText sourceText);
}