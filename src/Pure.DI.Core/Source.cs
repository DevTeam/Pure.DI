// ReSharper disable NotAccessedPositionalProperty.Global
namespace Pure.DI;

public readonly record struct Source(
    string HintName,
    SourceText SourceText);