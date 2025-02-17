namespace Pure.DI.Core.Models;

enum InjectionKind
{
    Field,

    Property,

    Parameter,

    Root,

    FactoryInjection,

    Contract,

    Construct
}