namespace Pure.DI.Core;

sealed class ConstructorInjectionsCounterWalker(ILocationProvider locationProvider)
    : DependenciesWalker<Unit>(locationProvider), IConstructorInjectionsCounterWalker
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
        in ImmutableArray<Location> locations,
        int? position) => Count++;
}