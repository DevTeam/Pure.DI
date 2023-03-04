namespace Pure.DI.Core.Models;

internal readonly record struct RewriterContext<T>(
    ITypeConstructor TypeConstructor,
    T State);