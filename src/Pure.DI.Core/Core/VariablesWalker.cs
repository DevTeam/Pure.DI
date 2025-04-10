namespace Pure.DI.Core;

sealed class VariablesWalker : DependenciesWalker<Unit>, IVariablesWalker
{
    private readonly List<Variable> _result = [];
    private ICollection<Variable> _variables = [];
    private Dictionary<Injection, LinkedList<Variable>> _variablesMap = [];

    public IVariablesWalker Initialize(ICollection<Variable> variables)
    {
        _variables = variables;
        _variablesMap = variables
            .GroupBy(i => i.Injection)
            .ToDictionary(i => i.Key, i => new LinkedList<Variable>(i));
        return this;
    }

    public IReadOnlyList<Variable> GetResult()
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
        if (_variablesMap.TryGetValue(injection, out var variables))
        {
            var variable = variables.First.Value;
            variables.RemoveFirst();
            _variables.Remove(variable);
            if (variables.Count == 0)
            {
                _variablesMap.Remove(injection);
            }

            _result.Add(variable);
        }

        base.VisitInjection(ctx, in injection, hasExplicitDefaultValue, explicitDefaultValue, locations, position);
    }
}