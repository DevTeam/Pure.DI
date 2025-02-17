namespace Pure.DI.Core.Code;

sealed class VariablesMap : Dictionary<MdBinding, Variable>, IVariablesMap
{
    public void Reset()
    {
        foreach (var variable in Values)
        {
            variable.Info.Reset();
        }

        var classBindings = this
            .Where(i => i.Value.Node.Arg is not null || i.Value.Node.Lifetime is not (Lifetime.Singleton or Lifetime.Scoped))
            .Select(i => i.Key)
            .ToList();

        foreach (var singletonBinding in classBindings)
        {
            Remove(singletonBinding);
        }
    }

    public IEnumerable<Variable> GetSingletons() => this
        .Where(i => i.Key.Lifetime?.Value is Lifetime.Singleton or Lifetime.Scoped)
        .Select(i => i.Value);

    public bool IsThreadSafe =>
        this.Any(i => i.Key.Lifetime?.Value is Lifetime.Singleton or Lifetime.Scoped or Lifetime.PerResolve);

    public IEnumerable<Variable> GetPerResolves() => this
        .Where(i => i.Key.Lifetime?.Value == Lifetime.PerResolve)
        .Select(i => i.Value);
}