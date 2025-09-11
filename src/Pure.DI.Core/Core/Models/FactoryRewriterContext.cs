namespace Pure.DI.Core.Models;

record FactoryRewriterContext(
    DpFactory Factory,
    in ImmutableArray<VarInjection> Parents,
    VarInjection VarInjection,
    string FinishLabel,
    ICollection<FactoryRewriter.Injection> Injections,
    ICollection<FactoryRewriter.Initializer> Initializers);