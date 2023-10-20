namespace Pure.DI.Core.Models;

internal readonly record struct InjectionInfo(
    Injection Injection,
    bool HasExplicitDefaultValue,
    object? ExplicitDefaultValue);