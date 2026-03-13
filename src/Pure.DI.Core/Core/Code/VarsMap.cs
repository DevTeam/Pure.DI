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
    : IVarsMap,
      IVarStateTracker
{
    private readonly Dictionary<int, Var> _map = [];
    private readonly HashSet<int> _perBlockBindingIds = [];
    private readonly List<ScopeState> _activeScopes = [];
    private int _suppressedTrackingCount;

    /// <inheritdoc />
    public IEnumerable<Var> Vars => _map.Values;

    /// <inheritdoc />
    public IEnumerable<VarDeclaration> Declarations => _map.Values.Select(i => i.Declaration);

    /// <inheritdoc />
    public bool IsThreadSafe { get; private set; }

    /// <inheritdoc />
    public VarInjection GetInjection(DependencyGraph graph, in Injection injection, IDependencyNode node)
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
                    var = CreateVar(graph, node, varTrace);
                    _map.Add(node.BindingId, var);
                    if (node.ActualLifetime == Lifetime.PerBlock)
                    {
                        _perBlockBindingIds.Add(node.BindingId);
                    }

                    RegisterAddedVar(node.BindingId);
                }
                varInjection = new VarInjection(var, injection);
                break;

            case Lifetime.Transient:
            default:
#if DEBUG
                varTrace = ImmutableArray.Create(trace.ToString());
#endif
                varInjection = new VarInjection(CreateVar(graph, node, varTrace), injection);
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
            var keysToRemove = new List<int>();
            foreach (var item in _map)
            {
                if (IsRootPersistentNode(item.Value.AbstractNode))
                {
                    continue;
                }

#if DEBUG
                lines.AppendLine($"// {item.Value.Declaration.Name}: remove ({nameof(Root)})");
#endif
                keysToRemove.Add(item.Key);
            }

            foreach (var key in keysToRemove)
            {
                _map.Remove(key);
            }

            _perBlockBindingIds.Clear();
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
        var scope = EnterScope(var.AbstractNode.BindingId);

        // Per-block variables should be isolated between local functions.
        List<KeyValuePair<int, Var>>? removed = null;
        foreach (var bindingId in _perBlockBindingIds)
        {
            if (!_map.TryGetValue(bindingId, out var perBlockVar))
            {
                continue;
            }

            if (!ShouldIsolatePerBlockVar(perBlockVar))
            {
                continue;
            }

#if DEBUG
            lines.AppendLine($"// {perBlockVar.Declaration.Name}: remove ({nameof(LocalFunction)})");
#endif
            (removed ??= new List<KeyValuePair<int, Var>>(_perBlockBindingIds.Count)).Add(new KeyValuePair<int, Var>(bindingId, perBlockVar));
            _map.Remove(bindingId);
        }

        return Disposables.Create(() => {
            _suppressedTrackingCount++;
            try
            {
                RemoveNewNonPersistentVars(var, scope, lines, nameof(LocalFunction));
                RestoreState(scope, lines, nameof(LocalFunction), false);
                if (removed is null)
                {
                    return;
                }

                foreach (var item in removed)
                {
#if DEBUG
                    lines.AppendLine($"// {item.Value.Declaration.Name}: rollback ({nameof(LocalFunction)})");
#endif
                    _map[item.Key] = item.Value;
                }
            }
            finally
            {
                _suppressedTrackingCount--;
                ExitScope(scope);
            }
        });
    }

    /// <inheritdoc />
    public IDisposable Lazy(Var var, Lines lines)
    {
        var scope = EnterScope(var.AbstractNode.BindingId);
        return Disposables.Create(() => {
            _suppressedTrackingCount++;
            try
            {
                RemoveNewNonPersistentVars(var, scope, lines, nameof(Lazy));
                RestoreState(scope, lines, nameof(Lazy), true);
            }
            finally
            {
                _suppressedTrackingCount--;
                ExitScope(scope);
            }
        });
    }

    /// <inheritdoc />
    public IDisposable Block(Var var, Lines lines)
    {
        var scope = EnterScope(var.AbstractNode.BindingId);

        // Per-block variables should be isolated between blocks.
        List<KeyValuePair<int, Var>>? removed = null;
        foreach (var bindingId in _perBlockBindingIds)
        {
            if (!_map.TryGetValue(bindingId, out var perBlockVar))
            {
                continue;
            }

            if (!ShouldIsolatePerBlockVar(perBlockVar))
            {
                continue;
            }

#if DEBUG
            lines.AppendLine($"// {perBlockVar.Declaration.Name}: remove ({nameof(Block)})");
#endif
            (removed ??= new List<KeyValuePair<int, Var>>(_perBlockBindingIds.Count)).Add(new KeyValuePair<int, Var>(bindingId, perBlockVar));
            _map.Remove(bindingId);
        }

        return Disposables.Create(() => {
            _suppressedTrackingCount++;
            try
            {
                RemoveNewNonPersistentVars(var, scope, lines, nameof(Block));
                RestoreState(scope, lines, nameof(Block), false);
                if (removed is null)
                {
                    return;
                }

                foreach (var item in removed)
                {
#if DEBUG
                    lines.AppendLine($"// {item.Value.Declaration.Name}: rollback ({nameof(Block)})");
#endif
                    _map[item.Key] = item.Value;
                }
            }
            finally
            {
                _suppressedTrackingCount--;
                ExitScope(scope);
            }
        });
    }

    private Var CreateVar(DependencyGraph graph, IDependencyNode node, ImmutableArray<string> trace)
    {
        var declaration = new VarDeclaration(nameProvider, this, idGenerator.Generate(), node);
        var var = new Var(graph, constructors, this, declaration, trace);
        return var;
    }

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

    private static bool ShouldIsolatePerBlockVar(Var perBlockVar) =>
        perBlockVar.AbstractNode.Construct is not { Source.Kind: MdConstructKind.Accumulator };

    private static bool ShouldKeepCurrentNodeInNestedScope(IDependencyNode node) =>
        node.ActualLifetime is Lifetime.PerBlock
        && node.Arg is not { Source.Kind: ArgKind.Root }
        && node.Construct is not { Source.Kind: MdConstructKind.Override };

    private ScopeState EnterScope(int excludeBindingId)
    {
        var scope = new ScopeState(excludeBindingId);
        _activeScopes.Add(scope);
        return scope;
    }

    private void ExitScope(ScopeState scope)
    {
        if (_activeScopes.Count > 0 && ReferenceEquals(_activeScopes[^1], scope))
        {
            _activeScopes.RemoveAt(_activeScopes.Count - 1);
            return;
        }

        _activeScopes.Remove(scope);
    }

    private void RegisterAddedVar(int bindingId)
    {
        foreach (var scope in _activeScopes)
        {
            scope.AddAddedBindingId(bindingId);
        }
    }

    void IVarStateTracker.OnStateChanging(int bindingId)
    {
        if (_suppressedTrackingCount > 0 || _activeScopes.Count == 0)
        {
            return;
        }

        if (!_map.TryGetValue(bindingId, out var var)
            || var.AbstractNode.Construct?.Source.Kind == MdConstructKind.Override)
        {
            return;
        }

        var state = new VarState(var);
        foreach (var scope in _activeScopes)
        {
            if (bindingId == scope.ExcludeBindingId
                || scope.ContainsAddedBindingId(bindingId)
                || scope.ContainsChangedBindingId(bindingId))
            {
                continue;
            }

            scope.AddChangedState(bindingId, state);
        }
    }

    /// <summary>
    /// Removes variables that were newly introduced in the nested scope and have non-persistent lifetimes
    /// (e.g., PerBlock or Transient). This prevents them from leaking into the parent scope.
    /// </summary>
    private void RemoveNewNonPersistentVars(Var var, ScopeState scope, Lines lines, string reason)
    {
#if DEBUG
        lines.AppendLine($"// remove new non-persistent vars ({reason} {var.Declaration.Name})");
#endif
        if (!scope.HasAddedBindingIds)
        {
            return;
        }

        List<KeyValuePair<int, Var>>? newItems = null;
        foreach (var bindingId in scope.AddedBindingIds)
        {
            if (!_map.TryGetValue(bindingId, out var addedVar))
            {
                continue;
            }

            var node = addedVar.AbstractNode;
            if (node.BindingId == scope.ExcludeBindingId)
            {
                if (!IsNestedScopePersistentNode(node)
                    && !ShouldKeepCurrentNodeInNestedScope(node))
                {
                    (newItems ??= []).Add(new KeyValuePair<int, Var>(bindingId, addedVar));
                }

                continue;
            }

            if (!IsNestedScopePersistentNode(node))
            {
                (newItems ??= []).Add(new KeyValuePair<int, Var>(bindingId, addedVar));
            }
        }

        if (newItems is null)
        {
            return;
        }

        foreach (var item in newItems)
        {
#if DEBUG
            lines.AppendLine($"//   {item.Value.Declaration.Name}");
#endif
            _map.Remove(item.Key);
        }
    }

    /// <summary>
    /// Restores the variables' state from the change log collected inside the current scope.
    /// </summary>
    private void RestoreState(ScopeState scope, Lines lines, string reason, bool restoreLocalFunctionCalled)
    {
#if DEBUG
        lines.AppendLine($"// restore state ({reason})");
#endif
        if (scope.HasChangedStates)
        {
            foreach (var stateItem in scope.ChangedStates)
            {
                var stateValue = stateItem.Value;
                var varToRestore = stateValue.Var;
#if DEBUG
                lines.AppendLine($"//   {varToRestore.Declaration.Name}");
#endif
                varToRestore.Declaration.RestoreDeclaredState(stateValue.IsDeclared);
                varToRestore.RestorePathState(
                    stateValue.IsCreated,
                    stateValue.IsLocalFunctionCalled,
                    stateValue.RawCodeExpression,
                    restoreLocalFunctionCalled);
            }
        }

        if (!scope.HasAddedBindingIds)
        {
            return;
        }

        foreach (var bindingId in scope.AddedBindingIds)
        {
            if (bindingId == scope.ExcludeBindingId || !_map.TryGetValue(bindingId, out var varToRestore))
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

    private readonly struct VarState(Var variable)
    {
        public readonly Var Var = variable;

        public readonly bool IsDeclared = variable.Declaration.IsDeclared;

        public readonly bool IsCreated = variable.IsCreated;

        public readonly bool IsLocalFunctionCalled = variable.IsLocalFunctionCalled;

        public readonly string? RawCodeExpression = variable.RawCodeExpression;
    }

    private sealed class ScopeState(int excludeBindingId)
    {
        private Dictionary<int, VarState>? _changedStates;
        private HashSet<int>? _addedBindingIds;

        public int ExcludeBindingId { get; } = excludeBindingId;

        public bool HasChangedStates => _changedStates is { Count: > 0 };

        public bool HasAddedBindingIds => _addedBindingIds is { Count: > 0 };

        public IEnumerable<KeyValuePair<int, VarState>> ChangedStates =>
            _changedStates is not null ? _changedStates : Array.Empty<KeyValuePair<int, VarState>>();

        public IEnumerable<int> AddedBindingIds =>
            _addedBindingIds is not null ? _addedBindingIds : Array.Empty<int>();

        public void AddChangedState(int bindingId, in VarState state) =>
            (_changedStates ??= new Dictionary<int, VarState>())[bindingId] = state;

        public void AddAddedBindingId(int bindingId) =>
            (_addedBindingIds ??= []).Add(bindingId);

        public bool ContainsChangedBindingId(int bindingId) =>
            _changedStates?.ContainsKey(bindingId) == true;

        public bool ContainsAddedBindingId(int bindingId) =>
            _addedBindingIds?.Contains(bindingId) == true;
    }
}
