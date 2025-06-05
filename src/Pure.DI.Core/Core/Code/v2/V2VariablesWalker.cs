namespace Pure.DI.Core.Code.v2;

sealed class V2VariablesWalker(ILocationProvider locationProvider)
    : DependenciesWalker<Unit>(locationProvider), IV2VariablesWalker
{
    private readonly List<Var> _result = [];
    private Dictionary<Injection, LinkedList<Var>> _varsMap = [];

    public IV2VariablesWalker Initialize(IReadOnlyCollection<Var> vars)
    {
        _varsMap = vars
            .GroupBy(var => var.Injection)
            .ToDictionary(i => i.Key, i => new LinkedList<Var>(i));
        return this;
    }

    public IReadOnlyList<Var> GetResult()
    {
        var result = _result.ToList();
        _result.Clear();
        return result;
    }

    public override void VisitInjection(
        in Unit ctx,
        in Injection injection,
        bool hasExplicitDefaultValue,
        object? explicitDefaultValue,
        in ImmutableArray<Location> locations,
        int? position)
    {
        if (_varsMap.TryGetValue(injection, out var vars))
        {
            var var = vars.First.Value;
            vars.RemoveFirst();
            if (vars.Count == 0)
            {
                _varsMap.Remove(injection);
            }

            _result.Add(var);
        }

        base.VisitInjection(ctx, in injection, hasExplicitDefaultValue, explicitDefaultValue, locations, position);
    }
}