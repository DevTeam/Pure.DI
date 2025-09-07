// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
namespace Pure.DI.Core.Code;

class VarsMap(
    [Tag(Tag.VarName)] IIdGenerator idGenerator,
    INameProvider nameProvider)
    : IVarsMap
{
    private readonly Dictionary<int, Var> _map = [];

    public IEnumerable<Var> Vars => _map.Values;

    public IEnumerable<VarDeclaration> Declarations => _map.Values.Select(i => i.Declaration);

    public bool IsThreadSafe { get; private set; }

    public VarInjection GetInjection(in Injection injection, IDependencyNode node)
    {
        VarInjection varInjection;
        switch (node.Lifetime)
        {
            case Lifetime.Singleton:
            case Lifetime.Scoped:
            case Lifetime.PerResolve:
            case Lifetime.PerBlock:
            case Lifetime.Transient when node.Arg is not null:
                if (!_map.TryGetValue(node.BindingId, out var var))
                {
                    var = new Var(CreateDeclaration(node));
                    _map.Add(node.BindingId, var);
                }
                varInjection = new VarInjection(var, injection);
                break;

            case Lifetime.Transient:
            default:
                varInjection = new VarInjection(new Var(CreateDeclaration(node)), injection);
                break;
        }

        IsThreadSafe |= node.Lifetime is Lifetime.Singleton or Lifetime.Scoped or Lifetime.PerResolve
                        || node.Arg is not null
                        || node.Construct is { Source.Kind: MdConstructKind.Accumulator };

        return varInjection;
    }

    public IDisposable Root(Lines lines)
    {
        return Disposables.Create(() => {
            _map
                .Where(i => !(i.Value.Declaration.Node.Lifetime is Lifetime.Singleton or Lifetime.Scoped || i.Value.Declaration.Node.Arg is { Source.Kind: ArgKind.Class }))
                .ToList()
                .ForEach(i => {
#if DEBUG
                    lines.AppendLine($"// {i.Value.Declaration.Name}: remove ({nameof(Root)})");
#endif
                    _map.Remove(i.Key);
                });

            foreach (var var in _map.Values)
            {
                var.Declaration.ResetToDefaults();
                var.ResetToDefaults();
                var.LocalFunction = new Lines();
                var.LocalFunctionName = "";
                var.CodeExpression = "";
                var.HasCycle = null;
            }
        });
    }

    public IDisposable LocalFunction(Var var, Lines lines)
    {
        var state = CreateState(var);
        // Remove per-block vars
        var removed = new List<KeyValuePair<int, Var>>();
        _map
            .Where(i => i.Value.Declaration.Node.Lifetime is Lifetime.PerBlock)
            .ToList()
            .ForEach(i => {
#if DEBUG
                lines.AppendLine($"// {i.Value.Declaration.Name}: remove ({nameof(Root)})");
#endif
                removed.Add(i);
                _map.Remove(i.Key);
            });

        // ResetVarsToDefaults(var, lines, nameof(LocalFunction));
        return Disposables.Create(() => {
            RemoveNewPerBlockVars(var, state, lines, nameof(LocalFunction));
            foreach (var item in removed)
            {
#if DEBUG
                lines.AppendLine($"// {item.Value.Declaration.Name}: rollback ({nameof(LocalFunction)})");
#endif
                _map[item.Key] = item.Value;
            }
        });
    }

    public IDisposable Lazy(Var var, Lines lines)
    {
        var state = CreateState(var);
        return Disposables.Create(() => {
            RemoveNewPerBlockVars(var, state, lines, nameof(Lazy));
            RestoreState(var, state, lines, nameof(Lazy));
        });
    }

    public IDisposable Block(Var var, Lines lines)
    {
        var state = CreateState(var);
        return Disposables.Create(() => {
            RemoveNewPerBlockVars(var, state, lines, nameof(Block));
        });
    }

    private VarDeclaration CreateDeclaration(IDependencyNode node) =>
        new(nameProvider, idGenerator.Generate(), node);

    private IReadOnlyDictionary<int, VarState> CreateState(Var var) =>
        _map
            .Where(i => i.Key != var.Declaration.Node.BindingId)
            .ToDictionary(i => i.Key, i => new VarState(i.Value));

    private void RemoveNewPerBlockVars(Var var, IReadOnlyDictionary<int, VarState> state, Lines lines, string reason)
    {
#if DEBUG
        lines.AppendLine($"// remove new per-block vars ({reason} {var.Declaration.Name})");
#endif
        var newItems = _map.Where(i => {
            if (state.ContainsKey(i.Key))
            {
                return false;
            }

            var node = i.Value.Declaration.Node;
            if (node.BindingId == var.Declaration.Node.BindingId)
            {
                return false;
            }

            return !(node.Lifetime is Lifetime.Singleton or Lifetime.Scoped or Lifetime.PerResolve || node.Arg is not null);
        }).ToList();

        foreach (var item in newItems)
        {
#if DEBUG
            lines.AppendLine($"//   {item.Value.Declaration.Name}");
#endif
            _map.Remove(item.Key);
        }
    }

    private void RestoreState(Var var, IReadOnlyDictionary<int, VarState> state, Lines lines, string reason)
    {
#if DEBUG
        lines.AppendLine($"// restore state ({reason} {var.Declaration.Name})");
#endif
        var restored = new HashSet<int>();
        foreach (var stateItem in state)
        {
            var varState = stateItem.Value;
            var varToRestore = varState.Var;
            if (varToRestore.Declaration.Node.BindingId == var.Declaration.Node.BindingId)
            {
                continue;
            }

            restored.Add(stateItem.Key);
            if (varToRestore.Declaration.IsDeclared == varState.IsDeclared && varToRestore.IsCreated == varState.IsCreated)
            {
                continue;
            }

#if DEBUG
            lines.AppendLine($"//   {varState.Var.Declaration.Name}");
#endif
            varToRestore.Declaration.IsDeclared = varState.IsDeclared;
            varToRestore.IsCreated = varState.IsCreated;
        }

        foreach (var varToRestore in _map.Where(i => !restored.Contains(i.Key)).Select(i => i.Value))
        {
            if (varToRestore.Declaration.Node.BindingId == var.Declaration.Node.BindingId)
            {
                continue;
            }

            var declarationReset = varToRestore.Declaration.ResetToDefaults();
            var varReset = varToRestore.ResetToDefaults();
            if (!declarationReset && !varReset)
            {
                continue;
            }

#if DEBUG
            lines.AppendLine($"//   {varToRestore.Declaration.Name}");
#endif
        }
    }

    private record VarState(Var Var)
    {
        public readonly bool IsDeclared = Var.Declaration.IsDeclared;

        public readonly bool IsCreated = Var.IsCreated;
    }
}