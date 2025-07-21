namespace Pure.DI.Core.Code;

sealed class VariablesWalker : DependenciesWalker<Unit>, IVariablesWalker
{
    private readonly List<VarInjection> _result = [];
    private readonly Dictionary<Injection, LinkedList<VarInjection>> _varInjectionsMap;

    public VariablesWalker(ILocationProvider locationProvider, IReadOnlyCollection<VarInjection> varInjections) : base(locationProvider)
    {
        _varInjectionsMap = varInjections
            .GroupBy(varInjection => varInjection.Injection)
            .ToDictionary(i => i.Key, i => new LinkedList<VarInjection>(i));
    }

    public IReadOnlyList<VarInjection> GetResult()
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
        if (_varInjectionsMap.TryGetValue(injection, out var vars))
        {
            var var = vars.First.Value;
            vars.RemoveFirst();
            if (vars.Count == 0)
            {
                _varInjectionsMap.Remove(injection);
            }

            _result.Add(var);
        }

        base.VisitInjection(ctx, in injection, hasExplicitDefaultValue, explicitDefaultValue, locations, position);
    }
}