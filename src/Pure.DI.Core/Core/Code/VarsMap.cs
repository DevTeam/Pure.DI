// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable UseCollectionExpression
namespace Pure.DI.Core.Code;

/// <summary>
/// Default implementation of <see cref="IVarsMap"/>.
/// </summary>
class VarsMap(
    [Tag(Tag.VarNameIdGenerator)] IIdGenerator idGenerator,
    INameProvider nameProvider,
    ICycleTools cycleTools,
    IConstructors constructors,
    [Tag(Tag.Local)] ICache<DependencyNode, bool> hasCycleCache)
    : IVarsMap,
      IVarStateTracker
{
    // The scope of a nested statement excludes no instance from tracking.
    private const int NoBindingId = int.MinValue;
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

        if (!hasCycleCache.TryGet(node.Node, out var hasCycle))
        {
            hasCycle = cycleTools.IsCyclic(graph.Graph, node.Node);
            hasCycleCache.Set(node.Node, hasCycle);
        }

        varInjection.Var.HasCycle = hasCycle;
        return varInjection;
    }

    /// <inheritdoc />
    public IDisposable Root(Lines lines)
    {
        var nameScope = nameProvider.Root();
        return Disposables.Create(() => {
            try
            {
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
            }
            finally
            {
                nameScope.Dispose();
            }
        });
    }

    /// <inheritdoc />
    public IDisposable LocalFunction(Var var, Lines lines, ImmutableArray<VarInjection> parameters = default)
    {
        var scope = EnterScope(var.AbstractNode.BindingId, var.AbstractNode.ActualLifetime is Lifetime.Singleton or Lifetime.Scoped);
        ForgetSharedVarsInCreation(var, lines, nameof(LocalFunction));
        var removed = RemovePerBlockVars(lines, nameof(LocalFunction));
        // A parameter can replace a variable of the caller, such as an accumulator, which the caller still sees after the function.
        List<KeyValuePair<int, Var>>? replaced = null;
        if (!parameters.IsDefaultOrEmpty)
        {
            foreach (var parameter in parameters)
            {
                var bindingId = parameter.Var.AbstractNode.BindingId;
                if (_map.TryGetValue(bindingId, out var callerVar))
                {
                    (replaced ??= []).Add(new KeyValuePair<int, Var>(bindingId, callerVar));
                }

                _map[bindingId] = parameter.Var;
            }
        }

        return Disposables.Create(() => {
            _suppressedTrackingCount++;
            try
            {
                RemoveNewNonPersistentVars(var, scope, lines, nameof(LocalFunction));
                RestoreState(scope, lines, nameof(LocalFunction), false);
                if (!parameters.IsDefaultOrEmpty)
                {
                    foreach (var parameter in parameters)
                    {
                        _map.Remove(parameter.Var.AbstractNode.BindingId);
                    }
                }

                if (replaced is not null)
                {
                    foreach (var item in replaced)
                    {
                        _map[item.Key] = item.Value;
                    }
                }

                RestorePerBlockVars(removed, lines, nameof(LocalFunction));
            }
            finally
            {
                _suppressedTrackingCount--;
                ExitScope(scope);
            }
        });
    }

    // A local function is shared by all its use sites, so its code can run outside the creation of the instance
    // it was generated under, for example, when a lambda in the code of a singleton leads to it, see #202.
    // A singleton or scoped instance whose local function is being generated counts as created only in its own code,
    // so the code of the function calls that local function, and the state before the function is restored after it.
    private void ForgetSharedVarsInCreation(Var var, Lines lines, string reason)
    {
        List<Var>? forgotten = null;
        foreach (var activeScope in _activeScopes)
        {
            if (!activeScope.IsSharedLocalFunction
                || activeScope.ExcludeBindingId == var.AbstractNode.BindingId
                || !_map.TryGetValue(activeScope.ExcludeBindingId, out var sharedVar)
                || !sharedVar.IsCreated)
            {
                continue;
            }

            (forgotten ??= []).Add(sharedVar);
        }

        if (forgotten is null)
        {
            return;
        }

        foreach (var forgottenVar in forgotten)
        {
#if DEBUG
            lines.AppendLine($"// {forgottenVar.Declaration.Name}: forget ({reason})");
#endif
            ((IVarStateTracker)this).OnStateChanging(forgottenVar.AbstractNode.BindingId);
            forgottenVar.ResetStateToDefaults(true);
        }
    }

    /// <inheritdoc />
    public IDisposable Lazy(Var var, Lines lines, in ImmutableArray<int> accumulatorBindingIds, Func<Var, bool>? isIsolated = null)
    {
        // Enumeration runs outside the block that creates it. Keep the current enumerable
        // so a cyclic element can call its iterator, but rebuild other per-block values inside it.
        // A lambda that overrides a value rebuilds the per-block values that read it.
        var removed = var.AbstractNode.Construct is { Source.Kind: MdConstructKind.Enumerable or MdConstructKind.AsyncEnumerable }
            ? RemovePerBlockVars(lines, nameof(Lazy), var.AbstractNode.BindingId, preserveActiveVars: true)
            : isIsolated is not null
                ? RemovePerBlockVars(lines, nameof(Lazy), var.AbstractNode.BindingId, preserveActiveVars: true, isIsolated)
                : null;
        var scope = IsolateAccumulators(var, lines, accumulatorBindingIds, nameof(Lazy), true);
        return Disposables.Create(() => {
            scope.Dispose();
            RestorePerBlockVars(removed, lines, nameof(Lazy));
        });
    }

    /// <inheritdoc />
    public IDisposable AccumulatorBoundary(Var var, Lines lines, in ImmutableArray<int> accumulatorBindingIds)
        => IsolateAccumulators(var, lines, accumulatorBindingIds, nameof(AccumulatorBoundary), false);

    private IDisposable IsolateAccumulators(
        Var var,
        Lines lines,
        in ImmutableArray<int> accumulatorBindingIds,
        string reason,
        bool restoreLocalFunctionCalled)
    {
        var scope = EnterScope(var.AbstractNode.BindingId);

        if (restoreLocalFunctionCalled)
        {
            // A deferred branch can run before any of its sibling branches. A singleton or
            // scoped instance created while generating a sibling must be checked again here.
            foreach (var existingVar in _map.Values)
            {
                if (existingVar.IsCreated
                    && existingVar.AbstractNode.ActualLifetime is Lifetime.Singleton or Lifetime.Scoped
                    && !_activeScopes.Any(active => active.ExcludeBindingId == existingVar.AbstractNode.BindingId))
                {
                    existingVar.ResetStateToDefaults(true);
                }
            }
        }

        // If the lazy graph injects an accumulator already used by its parent,
        // it defines a nested accumulation boundary. This is intentionally
        // based on accumulator metadata, not on a specific ownership wrapper.
        List<KeyValuePair<int, Var>>? removed = null;
        foreach (var bindingId in accumulatorBindingIds)
        {
            if (!_map.TryGetValue(bindingId, out var accumulatorVar))
            {
                continue;
            }

#if DEBUG
            lines.AppendLine($"// {accumulatorVar.Declaration.Name}: remove ({reason})");
#endif
            (removed ??= new List<KeyValuePair<int, Var>>(accumulatorBindingIds.Length))
                .Add(new KeyValuePair<int, Var>(bindingId, accumulatorVar));
            _map.Remove(bindingId);
        }

        return Disposables.Create(() => {
            _suppressedTrackingCount++;
            try
            {
                RemoveNewNonPersistentVars(var, scope, lines, reason);
                RestoreState(scope, lines, reason, restoreLocalFunctionCalled);
                if (removed is null)
                {
                    return;
                }

                foreach (var item in removed)
                {
#if DEBUG
                    lines.AppendLine($"// {item.Value.Declaration.Name}: rollback ({reason})");
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
    public IDisposable Block(Var var, Lines lines)
    {
        var scope = EnterScope(var.AbstractNode.BindingId);
        var removed = RemovePerBlockVars(lines, nameof(Block));

        return Disposables.Create(() => {
            _suppressedTrackingCount++;
            try
            {
                RemoveNewNonPersistentVars(var, scope, lines, nameof(Block));
                RestoreState(scope, lines, nameof(Block), false);
                RestorePerBlockVars(removed, lines, nameof(Block));
            }
            finally
            {
                _suppressedTrackingCount--;
                ExitScope(scope);
            }
        });
    }

    /// <inheritdoc />
    public IDisposable NestedStatement(Lines lines)
    {
        var scope = EnterScope(NoBindingId);
        return Disposables.Create(() => {
            _suppressedTrackingCount++;
            try
            {
                ForgetPerBlockVars(scope, lines);
            }
            finally
            {
                _suppressedTrackingCount--;
                ExitScope(scope);
            }
        });
    }

    // A PerBlock variable created in a nested statement is not seen after it. The code after the statement gets a new variable
    // with a name of its own, because C# does not allow a variable of a nested statement to have the name of a variable
    // declared after it in the enclosing block.
    private void ForgetPerBlockVars(ScopeState scope, Lines lines)
    {
#if DEBUG
        lines.AppendLine($"// forget PerBlock vars ({nameof(NestedStatement)})");
#endif
        HashSet<int>? forgotten = null;
        foreach (var item in scope.ChangedStates)
        {
            var state = item.Value;
            var var = state.Var;
            if (state.IsCreated
                || !var.IsCreated
                || !ShouldForgetPerBlockVar(var)
                || !_map.TryGetValue(item.Key, out var currentVar)
                || !ReferenceEquals(currentVar, var))
            {
                continue;
            }

            var.Declaration.RestoreDeclaredState(state.IsDeclared);
            var.RestorePathState(state.IsCreated, state.IsLocalFunctionCalled, state.RawCodeExpression, false);
            (forgotten ??= []).Add(item.Key);
        }

        foreach (var bindingId in scope.AddedBindingIds)
        {
            if (_map.TryGetValue(bindingId, out var var)
                && scope.ContainsAddedVar(bindingId, var)
                && var.IsCreated
                && ShouldForgetPerBlockVar(var))
            {
                (forgotten ??= []).Add(bindingId);
            }
        }

        if (forgotten is null)
        {
            return;
        }

        foreach (var bindingId in forgotten)
        {
#if DEBUG
            lines.AppendLine($"//   {_map[bindingId].Declaration.Name}");
#endif
            _map.Remove(bindingId);
        }
    }

    private static bool ShouldForgetPerBlockVar(Var var) =>
        var.AbstractNode is { ActualLifetime: Lifetime.PerBlock, Arg: null }
        && var.AbstractNode.Construct is not { Source.Kind: MdConstructKind.Accumulator or MdConstructKind.Override };

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

    private List<KeyValuePair<int, Var>>? RemovePerBlockVars(
        Lines lines,
        string reason,
        int? retainedBindingId = null,
        bool preserveActiveVars = false,
        Func<Var, bool>? isRemoved = null)
    {
        List<KeyValuePair<int, Var>>? removed = null;
        foreach (var bindingId in _perBlockBindingIds)
        {
            // A dependency still being built must remain visible to nested enumerables.
            // Removing an active Func<T> or enumerable expands a cyclic branch indefinitely.
            if (bindingId == retainedBindingId
                || !_map.TryGetValue(bindingId, out var perBlockVar)
                || !ShouldIsolatePerBlockVar(perBlockVar)
                || (preserveActiveVars && _activeScopes.Any(scope => scope.ExcludeBindingId == bindingId))
                || isRemoved is not null && !isRemoved(perBlockVar))
            {
                continue;
            }

#if DEBUG
            lines.AppendLine($"// {perBlockVar.Declaration.Name}: remove ({reason})");
#endif
            (removed ??= new List<KeyValuePair<int, Var>>(_perBlockBindingIds.Count))
                .Add(new KeyValuePair<int, Var>(bindingId, perBlockVar));
            _map.Remove(bindingId);
        }

        return removed;
    }

    private void RestorePerBlockVars(List<KeyValuePair<int, Var>>? removed, Lines lines, string reason)
    {
        if (removed is null)
        {
            return;
        }

        foreach (var item in removed)
        {
#if DEBUG
            lines.AppendLine($"// {item.Value.Declaration.Name}: rollback ({reason})");
#endif
            _map[item.Key] = item.Value;
        }
    }

    private static bool ShouldKeepCurrentNodeInNestedScope(IDependencyNode node) =>
        node.ActualLifetime is Lifetime.PerBlock
        && node.Arg is not { Source.Kind: ArgKind.Root }
        && node.Construct is not { Source.Kind: MdConstructKind.Override };

    private ScopeState EnterScope(int excludeBindingId, bool isSharedLocalFunction = false)
    {
        var scope = new ScopeState(excludeBindingId, isSharedLocalFunction);
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
            scope.AddAddedVar(bindingId, _map[bindingId]);
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
                || scope.ContainsAddedVar(bindingId, var)
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

            // An inner boundary may have replaced this binding and then restored the outer
            // accumulator. That restored variable was not introduced in this scope, see #190.
            if (!scope.ContainsAddedVar(bindingId, addedVar))
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
            if (bindingId == scope.ExcludeBindingId
                || !_map.TryGetValue(bindingId, out var varToRestore)
                || !scope.ContainsAddedVar(bindingId, varToRestore))
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

    private sealed class ScopeState(int excludeBindingId, bool isSharedLocalFunction)
    {
        private Dictionary<int, VarState>? _changedStates;
        private HashSet<int>? _addedBindingIds;
        private HashSet<(int BindingId, int VariableId)>? _addedVariables;

        public int ExcludeBindingId { get; } = excludeBindingId;

        // The scope of the local function that creates a singleton or scoped instance.
        public bool IsSharedLocalFunction { get; } = isSharedLocalFunction;

        public bool HasChangedStates => _changedStates is { Count: > 0 };

        public bool HasAddedBindingIds => _addedBindingIds is { Count: > 0 };

        public IEnumerable<KeyValuePair<int, VarState>> ChangedStates =>
            _changedStates is not null ? _changedStates : Array.Empty<KeyValuePair<int, VarState>>();

        public IEnumerable<int> AddedBindingIds =>
            _addedBindingIds is not null ? _addedBindingIds : Array.Empty<int>();

        public void AddChangedState(int bindingId, in VarState state) =>
            (_changedStates ??= new Dictionary<int, VarState>())[bindingId] = state;

        public void AddAddedVar(int bindingId, Var var)
        {
            (_addedBindingIds ??= []).Add(bindingId);
            (_addedVariables ??= []).Add((bindingId, var.Declaration.PerLifetimeId));
        }

        public bool ContainsChangedBindingId(int bindingId) =>
            _changedStates?.ContainsKey(bindingId) == true;

        public bool ContainsAddedVar(int bindingId, Var var) =>
            _addedVariables?.Contains((bindingId, var.Declaration.PerLifetimeId)) == true;
    }
}
