namespace Pure.DI.Core.Models;

readonly record struct InjectionInfo(
    Injection Injection,
    bool HasExplicitDefaultValue,
    object? ExplicitDefaultValue);