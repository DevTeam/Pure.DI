namespace Pure.DI.Core;

internal record DependencyNodeBuildContext(
    MdSetup Setup,
    ITypeConstructor TypeConstructor);