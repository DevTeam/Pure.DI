namespace Pure.DI.Core;

internal interface ISources
{
    void AddSource(string hintName, SourceText sourceText);
}