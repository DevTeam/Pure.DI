// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
namespace Pure.DI.Core.Code;

/// <summary>
/// Default implementation of <see cref="IVarsMap"/>.
/// </summary>
class VarsMap(
    [Tag(Tag.VarNameIdGenerator)] IIdGenerator idGenerator,
    INameProvider nameProvider,
    ICycleTools cycleTools,
    IConstructors constructors)
    : IVarsMap
{
    private readonly Dictionary<int, Var> _map = [];

    /// <inheritdoc />
    public IEnumerable<Var> Vars => _map.Values;

    /// <inheritdoc />
    public IEnumerable<VarDeclaration> Declarations => _map.Values.Select(i => i.Declaration);

    /// <inheritdoc />
    public bool IsThreadSafe { get; private set; }

    /// <inheritdoc />
    public VarInjection GetInjection(DependencyGraph graph, Root root, in Injection injection, IDependencyNode node)
    {
        VarInjection varInjection;
        var trace = new StringBuilder();
        ImmutableArray<string> varTrace;
        switch (node.ActualLifetime)
        {
            case Lifetime.Singleton:
            case Lifetime.Scoped:
            case Lifetime.PerResolve:
            case Lifetime.PerBlock:
            case Lifetime.Transient when node.Arg is not null:
                if (!_map.TryGetValue(node.BindingId, out var var))
                {
#if DEBUG
                    varTrace = ImmutableArray.Create(trace.ToString());
#endif
                    var = new Var(graph, constructors, CreateDeclaration(node), varTrace);
                    _map.Add(node.BindingId, var);
                }
                varInjection = new VarInjection(var, injection);
                break;

            case Lifetime.Transient:
            default:
#if DEBUG
                varTrace = ImmutableArray.Create(trace.ToString());
#endif
                varInjection = new VarInjection(new Var(graph, constructors, CreateDeclaration(node), varTrace), injection);
                break;
        }

        IsThreadSafe |= IsThreadSafeNode(node);

        varInjection.Var.HasCycle = cycleTools.GetCyclicNode(graph.Graph, node.Node) == node.Node;
        return varInjection;
    }

    /// <inheritdoc />
    public IDisposable Root(Lines lines)
    {
        return Disposables.Create(() => {
            _map
                .Where(i => !IsRootPersistentNode(i.Value.Declaration.Node))
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
            }

            IsThreadSafe = false;
        });
    }

    /// <inheritdoc />
    public IDisposable LocalFunction(Var var, Lines lines)
    {
        // Snapshot the current state before entering a local function.
        var state = CreateState(var);
        
        // Per-block variables should be isolated between local functions.
        var removed = new List<KeyValuePair<int, Var>>();
        _map
            .Where(i => i.Value.Declaration.Node.ActualLifetime is Lifetime.PerBlock)
            .ToList()
            .ForEach(i => {
#if DEBUG
                lines.AppendLine($"// {i.Value.Declaration.Name}: remove ({nameof(LocalFunction)})");
#endif
                removed.Add(i);
                _map.Remove(i.Key);
            });

        return Disposables.Create(() => {
            // Cleanup and restore state after exiting the local function.
            RemoveNewNonPersistentVars(var, state, lines, nameof(LocalFunction));
            RestoreState(var, state, lines, nameof(LocalFunction), false);
            foreach (var item in removed)
            {
#if DEBUG
                lines.AppendLine($"// {item.Value.Declaration.Name}: rollback ({nameof(LocalFunction)})");
#endif
                _map[item.Key] = item.Value;
            }
        });
    }

    /// <inheritdoc />
    public IDisposable Lazy(Var var, Lines lines)
    {
        // Snapshot the state before entering a lazy scope (e.g., Func<T>).
        var state = CreateState(var);
        return Disposables.Create(() => {
            // Cleanup and restore state after exiting the lazy scope.
            RemoveNewNonPersistentVars(var, state, lines, nameof(Lazy));
            RestoreState(var, state, lines, nameof(Lazy), true);
        });
    }

    /// <inheritdoc />
    public IDisposable Block(Var var, Lines lines)
    {
        // Snapshot the state before entering a code block.
        var state = CreateState(var);
        return Disposables.Create(() => {
            // Cleanup and restore state after exiting the block.
            RemoveNewNonPersistentVars(var, state, lines, nameof(Block));
            RestoreState(var, state, lines, nameof(Block), false);
        });
    }

    private VarDeclaration CreateDeclaration(IDependencyNode node) =>
        new(nameProvider, idGenerator.Generate(), node);

    private static bool IsThreadSafeNode(IDependencyNode node) =>
        node.ActualLifetime is Lifetime.Singleton or Lifetime.Scoped or Lifetime.PerResolve
        || node.Arg is not null
        || node.Construct is { Source.Kind: MdConstructKind.Accumulator };

    private static bool IsRootPersistentNode(IDependencyNode node) =>
        node.ActualLifetime is Lifetime.Singleton or Lifetime.Scoped
        || node.Arg is { Source.Kind: ArgKind.Composition };

    private static bool IsNestedScopePersistentNode(IDependencyNode node) =>
        node.ActualLifetime is Lifetime.Singleton or Lifetime.Scoped or Lifetime.PerResolve
        || node.Arg is not null;

    private static bool ShouldKeepCurrentNodeInNestedScope(IDependencyNode node) =>
        node.ActualLifetime is Lifetime.PerBlock
        && node.Arg is not { Source.Kind: ArgKind.Root }
        && node.Construct is not { Source.Kind: MdConstructKind.Override };

    /// <summary>
    /// Creates a snapshot of the current variables' state.
    /// Global code state (like LocalFunction) is NOT part of the snapshot because it should accumulate.
    /// Only path-specific state (IsDeclared, IsCreated, CodeExpression) is captured.
    /// </summary>
    private IReadOnlyDictionary<int, VarState> CreateState(Var var) =>
        _map
            .Where(i => i.Key != var.Declaration.Node.BindingId
                        && i.Value.Declaration.Node.Construct is not { Source.Kind: MdConstructKind.Override })
            .ToDictionary(i => i.Key, i => new VarState(i.Value));

    /// <summary>
    /// Removes variables that were newly introduced in the nested scope and have non-persistent lifetimes
    /// (e.g., PerBlock or Transient). This prevents them from leaking into the parent scope.
    /// </summary>
    private void RemoveNewNonPersistentVars(Var var, IReadOnlyDictionary<int, VarState> state, Lines lines, string reason)
    {
#if DEBUG
        lines.AppendLine($"// remove new non-persistent vars ({reason} {var.Declaration.Name})");
#endif
        var newItems = _map.Where(i => {
            if (state.ContainsKey(i.Key))
            {
                return false;
            }

            var node = i.Value.Declaration.Node;
            if (node.BindingId == var.Declaration.Node.BindingId)
            {
                return !IsNestedScopePersistentNode(node)
                       && !ShouldKeepCurrentNodeInNestedScope(node);
            }

            return !IsNestedScopePersistentNode(node);
        }).ToList();

        foreach (var item in newItems)
        {
#if DEBUG
            lines.AppendLine($"//   {item.Value.Declaration.Name}");
#endif
            _map.Remove(item.Key);
        }
    }

    /// <summary>
    /// Restores the variables' state from a previously taken snapshot.
    /// </summary>
    private void RestoreState(Var var, IReadOnlyDictionary<int, VarState> state, Lines lines, string reason, bool restoreLocalFunctionCalled)
    {
#if DEBUG
        lines.AppendLine($"// restore state ({reason} {var.Declaration.Name})");
#endif
        var restored = new HashSet<int>();
        foreach (var stateItem in state)
        {
            var (varToRestore, isDeclared, isCreated, isLocalFunctionCalled, codeExpression) = stateItem.Value;
            if (varToRestore.Declaration.Node.BindingId == var.Declaration.Node.BindingId)
            {
                continue;
            }

            restored.Add(stateItem.Key);
            
            // Only restore path-specific state. 
            // Global state (like LocalFunction) should persist even after exiting a nested scope.
            if (varToRestore.Declaration.IsDeclared == isDeclared
                && varToRestore.IsCreated == isCreated
                && (!restoreLocalFunctionCalled || varToRestore.IsLocalFunctionCalled == isLocalFunctionCalled)
                && varToRestore.CodeExpression == codeExpression)
            {
                continue;
            }

#if DEBUG
            lines.AppendLine($"//   {varToRestore.Declaration.Name}");
#endif
            varToRestore.Declaration.IsDeclared = isDeclared;
            varToRestore.IsCreated = isCreated;
            if (restoreLocalFunctionCalled)
            {
                varToRestore.IsLocalFunctionCalled = isLocalFunctionCalled;
            }
            varToRestore.CodeExpression = codeExpression;
        }

        // For variables that were NOT in the snapshot (newly discovered in the nested scope),
        // we reset their path-specific state to defaults.
        // This ensures that if they are needed again in the parent scope, they will be properly initialized.
        foreach (var varToRestore in _map.Where(i => !restored.Contains(i.Key)).Select(i => i.Value))
        {
            if (varToRestore.Declaration.Node.BindingId == var.Declaration.Node.BindingId)
            {
                continue;
            }

            var declarationReset = varToRestore.Declaration.ResetStateToDefaults();
            var varReset = varToRestore.ResetStateToDefaults(restoreLocalFunctionCalled);
            if (!declarationReset && !varReset)
            {
                continue;
            }

#if DEBUG
            lines.AppendLine($"//   {varToRestore.Declaration.Name}");
#endif
        }
    }

    private record VarState(
        Var Var,
        bool IsDeclared,
        bool IsCreated,
        bool IsLocalFunctionCalled,
        string CodeExpression)
    {
        public VarState(Var variable)
            : this(
                variable,
                variable.Declaration.IsDeclared,
                variable.IsCreated,
                variable.IsLocalFunctionCalled,
                variable.CodeExpression)
        {
        }
    }
}
