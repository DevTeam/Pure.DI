namespace Pure.DI.Core;

interface IBindingsFactory
{
    MdBinding CreateGenericBinding(
        MdSetup setup,
        Injection injection,
        DependencyNode sourceNode,
        ITypeConstructor typeConstructor,
        int newId);

    MdBinding CreateAccumulatorBinding(
        MdSetup setup,
        DependencyNode targetNode,
        ref int newId,
        IReadOnlyCollection<MdAccumulator> accumulators,
        bool hasExplicitDefaultValue,
        object? explicitDefaultValue);

    MdBinding CreateAutoBinding(
        MdSetup setup,
        DependencyNode targetNode,
        Injection injection,
        ITypeConstructor typeConstructor,
        int newId);

    MdBinding CreateConstructBinding(
        MdSetup setup,
        DependencyNode targetNode,
        Injection injection,
        ITypeSymbol elementType,
        Lifetime lifetime,
        ITypeConstructor typeConstructor,
        int newId,
        MdConstructKind constructKind,
        object? tag = null,
        bool hasExplicitDefaultValue = false,
        object? explicitDefaultValue = null,
        object? state = null);
}