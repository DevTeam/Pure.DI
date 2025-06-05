namespace Pure.DI.Core.Code.v2;

record VarKey(MdBinding Binding, DeclarationPath Path);

class VarsMap(
    IVariableNameProvider variableNameProvider,
    [Tag(Tag.VarName)] IdGenerator idGenerator)
    : Dictionary<VarKey, VarDeclaration>, IVarsMap
{
    public IEnumerable<MdBinding> GetBindings() => Keys.Select(i => i.Binding);

    public IEnumerable<VarDeclaration> GetSingletons() =>
        Sort(Values.Where(declaration => declaration.Node.Lifetime is Lifetime.Singleton or Lifetime.Scoped));

    public IEnumerable<VarDeclaration> GetPerResolves() =>
        Sort(Values.Where(declaration => declaration.Node.Lifetime is Lifetime.PerResolve));

    public IEnumerable<VarDeclaration> GetArgs() =>
        Sort(Values.Where(declaration => declaration.Node.Arg is not null));

    public Var GetVar(in Injection injection, DependencyNode node, DeclarationPath path)
    {
        var inMap = node.Lifetime != Lifetime.Transient || node.Arg is not null;
        if (!inMap)
        {
            return new Var(CreateNewDeclaration(node), injection);
        }

        var actualPath = node.Lifetime == Lifetime.PerBlock ? path : DeclarationPath.Root;
        var key = new VarKey(node.Binding, actualPath);
        if (TryGetValue(key, out var declaration))
        {
            return new Var(declaration, injection);
        }

        var newDeclaration = CreateNewDeclaration(node);
        Add(key, newDeclaration);
        return new Var(newDeclaration, injection);
    }

    public CodeChanges Reset(CodeChanges changes, bool resetSingletons, DeclarationPath path)
    {
        var changedNodes = changes.Declarations.Select(i => i.Node).ToImmutableHashSet();
        foreach (var item in this)
        {
            if (!changedNodes.Contains(item.Value.Node))
            {
                continue;
            }

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (item.Key.Binding.Lifetime?.Value ?? Lifetime.Transient)
            {
                case Lifetime.Singleton:
                case Lifetime.Scoped:
                case Lifetime.PerResolve:
                    if (!resetSingletons)
                    {
                        continue;
                    }
                    break;

                case Lifetime.PerBlock:
                    var itemPath = item.Key.Path;
                    if (itemPath.GetHashCode() != path.GetHashCode() || !itemPath.Equals(path))
                    {
                        continue;
                    }

                    break;
            }

            item.Value.Reset();
        }

        return changes;
    }

    public void Remove(IEnumerable<MdBinding> bindings)
    {
        var bindingsToRemove = bindings.ToImmutableHashSet();
        var keysToRemove = Keys.Where(key => bindingsToRemove.Contains(key.Binding)).ToList();
        foreach (var keyToRemove in keysToRemove)
        {
            Remove(keyToRemove);
        }
    }

    public IEnumerable<VarDeclaration> Sort(IEnumerable<VarDeclaration> declarations) =>
        declarations
            .GroupBy(i => i.Node.Binding.Id)
            .Select(i => i.First())
            .OrderBy(i => !(i.Node.Arg?.Source.IsBuildUpInstance ?? false))
            .ThenBy(i => i.Node.Binding.Id);

    private VarDeclaration CreateNewDeclaration(DependencyNode node) =>
        new(variableNameProvider, idGenerator.Generate(), node);
}