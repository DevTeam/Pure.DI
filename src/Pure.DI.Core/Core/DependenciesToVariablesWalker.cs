namespace Pure.DI.Core;

internal sealed class DependenciesToVariablesWalker: DependenciesWalker
{
    private readonly IList<Variable> _variables;
    private readonly Dictionary<Injection, List<Variable>> _variablesMap;
    private readonly ImmutableArray<Variable>.Builder _resultBuilder = ImmutableArray.CreateBuilder<Variable>();

    public DependenciesToVariablesWalker(IList<Variable> variables)
    {
        _variables = variables;
        _variablesMap = variables
            .GroupBy(i => i.Injection)
            .ToDictionary(i => i.Key, i => i.ToList());
    }

    public ImmutableArray<Variable> GetResult()
    {
        var result = _resultBuilder.ToImmutable();
        _resultBuilder.Clear();
        return result;
    }

    public override void VisitInjection(in Injection injection, in ImmutableArray<Location> locations)
    {
        if (_variablesMap.TryGetValue(injection, out var variables))
        {
            var variable = variables[^1];
            variables.RemoveAt(variables.Count - 1);
            _variables.Remove(variable);
            if (variables.Count == 0)
            {
                _variablesMap.Remove(injection);
            }
            
            _resultBuilder.Add(variable);
        }
        
        base.VisitInjection(in injection, locations);
    }
}