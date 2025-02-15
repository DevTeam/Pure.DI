namespace Pure.DI.Core;

internal sealed class ConstructorInjectionsCounterWalker : DependenciesWalker<Unit>, IConstructorInjectionsCounterWalker
{
    public int Count { get; private set; }

    public override void VisitConstructor(in Unit ctx, in DpMethod constructor)
    {
        Count = 0;
        base.VisitConstructor(in ctx, in constructor);
    }

    public override void VisitInjection(
        in Unit ctx,
        in Injection injection,
        bool hasExplicitDefaultValue,
        object? explicitDefaultValue,
        in ImmutableArray<Location> locations) => Count++;
}