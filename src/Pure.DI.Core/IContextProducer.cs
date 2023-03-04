namespace Pure.DI;

public interface IContextProducer
{
    void AddSource(string hintName, SourceText sourceText);
}