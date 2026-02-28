namespace Pure.DI.Core.Models;

record DependencyNodeBuildContext(
    MdSetup Setup,
    MdSetup OriginalSetup,
    ITypeConstructor TypeConstructor);