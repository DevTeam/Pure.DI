namespace Pure.DI.Core.Models;

record FactoryRewriterContext(
    DpFactory Factory,
    VarInjection VarInjection,
    string FinishLabel,
    ICollection<FactoryRewriter.Injection> Injections,
    ICollection<FactoryRewriter.Initializer> Initializers);