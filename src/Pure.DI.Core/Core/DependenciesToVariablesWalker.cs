namespace Pure.DI.Core;

using System.Collections;

internal class DependenciesToVariablesWalker: DependenciesWalker, IEnumerable<Variable>
{
    private readonly IList<Variable> _variables;
    private readonly List<Variable> _result = new();

    public DependenciesToVariablesWalker(IList<Variable> variables)
    {
        _variables = variables;
    }

    public override void VisitInjection(in Injection injection)
    {
        for (var i = 0; i < _variables.Count(); i++)
        {
            var variable = _variables[i];
            if (!variable.Injection.Equals(injection))
            {
                continue;
            }

            _variables.RemoveAt(i);
            _result.Add(variable);
        }
        
        base.VisitInjection(in injection);
    }


    public IEnumerator<Variable> GetEnumerator() => _result.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}