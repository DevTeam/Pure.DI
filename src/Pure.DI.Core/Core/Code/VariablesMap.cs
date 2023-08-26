namespace Pure.DI.Core.Code;

internal class VariablesMap: Dictionary<MdBinding, Variable>
{
    public void Reset()
    {
        var classBindings = this
            .Where(i => i.Value.Node.Arg is not null || i.Value.Node.Lifetime != Lifetime.Singleton)
            .Select(i => i.Key)
            .ToArray();

        foreach (var singletonBinding in classBindings)
        {
            Remove(singletonBinding);
        }

        foreach (var variable in Values)
        {
            variable.Reset();
        }
    }

    public IEnumerable<Variable> GetSingletons() => this
        .Where(i => i.Key.Lifetime?.Value == Lifetime.Singleton)
        .Select(i => i.Value);
    
    public IEnumerable<Variable> GetPerResolves() => this
        .Where(i => i.Key.Lifetime?.Value == Lifetime.PerResolve)
        .Select(i => i.Value);
}