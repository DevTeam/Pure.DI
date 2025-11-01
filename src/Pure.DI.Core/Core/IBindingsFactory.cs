namespace Pure.DI.Core;

interface IBindingsFactory
{
    MdBinding CreateGenericBinding(
        MdSetup setup,
        Injection injection,
        DependencyNode sourceNode,
        ITypeConstructor typeConstructor,
        int bindingId);

    MdBinding CreateAccumulatorBinding(
        MdSetup setup,
        DependencyNode targetNode,
        ref int bindingId,
        IReadOnlyCollection<MdAccumulator> accumulators,
        bool hasExplicitDefaultValue,
        object? explicitDefaultValue);

    MdBinding CreateAutoBinding(
        MdSetup setup,
        DependencyNode targetNode,
        Injection injection,
        ITypeConstructor typeConstructor,
        int bindingId);

    MdBinding CreateConstructBinding(
        MdSetup setup,
        DependencyNode targetNode,
        Injection injection,
        ITypeSymbol elementType,
        Lifetime lifetime,
        ITypeConstructor typeConstructor,
        int bindingId,
        MdConstructKind constructKind,
        object? tag = null,
        bool hasExplicitDefaultValue = false,
        object? explicitDefaultValue = null,
        object? state = null);
}