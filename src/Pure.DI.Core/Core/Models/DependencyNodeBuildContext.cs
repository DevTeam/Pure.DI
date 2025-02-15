namespace Pure.DI.Core.Models;

internal record DependencyNodeBuildContext(
    MdSetup Setup,
    ITypeConstructor TypeConstructor);