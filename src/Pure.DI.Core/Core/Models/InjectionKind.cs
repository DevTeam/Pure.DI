namespace Pure.DI.Core.Models;

internal enum InjectionKind
{
    Field,
    Property,
    Parameter,
    Root,
    Injection,
    Contract,
    Construct
}