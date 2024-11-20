namespace Pure.DI.Core;

internal sealed class DependenciesToVariablesWalker : DependenciesWalker<Unit>
{
    private readonly ICollection<Variable> _variables;
    private readonly Dictionary<Injection, LinkedList<Variable>> _variablesMap;
    private readonly ImmutableArray<Variable>.Builder _resultBuilder = ImmutableArray.CreateBuilder<Variable>();

    public DependenciesToVariablesWalker(ICollection<Variable> variables)
    {
        _variables = variables;
        _variablesMap = variables
            .GroupBy(i => i.Injection)
            .ToDictionary(i => i.Key, i => new LinkedList<Variable>(i));
    }

    public IReadOnlyList<Variable> GetResult()
    {
        var result = _resultBuilder.ToList();
        _resultBuilder.Clear();
        return result;
    }

    public override void VisitInjection(
        in Unit ctx,
        in Injection injection,
        bool hasExplicitDefaultValue,
        object? explicitDefaultValue,
        in ImmutableArray<Location> locations)
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

            _resultBuilder.Add(variable);
        }

        base.VisitInjection(ctx, in injection, hasExplicitDefaultValue, explicitDefaultValue, locations);
    }
}