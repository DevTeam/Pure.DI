namespace Pure.DI.Core.Models;

internal readonly record struct RewriterContext<T>(
    ITypeConstructor TypeConstructor,
    in Injection Injection,
    T State);