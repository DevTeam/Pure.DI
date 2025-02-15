namespace Pure.DI.Core;

internal sealed class InjectionsWalker : DependenciesWalker<Unit>, IInjectionsWalker
{
    private readonly List<InjectionInfo> _result = [];

    public override void VisitInjection(
        in Unit ctx,
        in Injection injection,
        bool hasExplicitDefaultValue,
        object? explicitDefaultValue,
        in ImmutableArray<Location> locations)
    {
        _result.Add(new InjectionInfo(injection, hasExplicitDefaultValue, explicitDefaultValue));
        base.VisitInjection(Unit.Shared, in injection, hasExplicitDefaultValue, explicitDefaultValue, locations);
    }

    public IReadOnlyCollection<InjectionInfo> GetResult() => _result;
}