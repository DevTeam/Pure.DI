namespace Pure.DI.Core.Code;

sealed class VariablesWalker : DependenciesWalker<Unit>, IVariablesWalker
{
    private readonly List<VarInjection> _result = [];
    private readonly ICache<Injection, Queue<VarInjection>> _varInjectionsMap;

    public VariablesWalker(
        ILocationProvider locationProvider,
        IEnumerable<VarInjection> varInjections,
        [Tag(Tag.Local)] ICache<Injection, Queue<VarInjection>> varInjectionsMap)
        : base(locationProvider)
    {
        _varInjectionsMap = varInjectionsMap;
        foreach (var varInjection in varInjections)
        {
            if (!_varInjectionsMap.TryGet(varInjection.Injection, out var injections))
            {
                injections = new Queue<VarInjection>();
                _varInjectionsMap.Set(varInjection.Injection, injections);
            }

            injections.Enqueue(varInjection);
        }
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
        if (_varInjectionsMap.TryGet(injection, out var vars))
        {
            var var = vars.Dequeue();
            if (vars.Count == 0)
            {
                _varInjectionsMap.Remove(injection);
            }

            _result.Add(var);
        }

        base.VisitInjection(ctx, in injection, hasExplicitDefaultValue, explicitDefaultValue, locations, position);
    }
}
