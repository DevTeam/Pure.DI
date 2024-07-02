namespace Pure.DI.Core.Models;

internal readonly record struct RewriterContext<T>(
    MdSetup Setup,
    ITypeConstructor TypeConstructor,
    in Injection Injection,
    T State);