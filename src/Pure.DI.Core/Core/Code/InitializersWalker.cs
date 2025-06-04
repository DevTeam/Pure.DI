// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident
namespace Pure.DI.Core.Code;

sealed class InitializersWalker(
    IInjections injections, ILocationProvider locationProvider)
    : DependenciesWalker<CodeContext>(locationProvider), IInitializersWalker
{
    private readonly List<(Action Run, int? Ordinal)> _actions = [];
    private readonly List<VarInjection> _varInjections = [];
    private Action<VarInjection>? _buildVarInjection;
    private string _varName = string.Empty;
    private IEnumerator<VarInjection> _varInjectionEnumerator = Enumerable.Empty<VarInjection>().GetEnumerator();

    public IInitializersWalker Ininitialize(Action<VarInjection> buildVarInjection, string variableName, IEnumerator<VarInjection> varInjections)
    {
        _buildVarInjection = buildVarInjection;
        _varName = variableName;
        _varInjectionEnumerator = varInjections;
        return this;
    }

    public override void VisitInitializer(in CodeContext ctx, DpInitializer initializer)
    {
        base.VisitInitializer(in ctx, initializer);
        foreach (var action in _actions.OrderBy(i => i.Ordinal ?? int.MaxValue).Select(i => i.Run))
        {
            action();
        }
    }

    public override void VisitInjection(in CodeContext ctx, in Injection injection, bool hasExplicitDefaultValue, object? explicitDefaultValue, in ImmutableArray<Location> locations, int? position)
    {
        if (_varInjectionEnumerator.MoveNext())
        {
            _buildVarInjection?.Invoke(_varInjectionEnumerator.Current);
            _varInjections.Add(_varInjectionEnumerator.Current);
        }

        base.VisitInjection(in ctx, in injection, hasExplicitDefaultValue, explicitDefaultValue, in locations, position);
    }

    public override void VisitMethod(in CodeContext ctx, in DpMethod method, int? position)
    {
        base.VisitMethod(in ctx, in method, position);
        var curCtx = ctx;
        var curMethod = method;
        var curVariables = _varInjections.ToList();
        _actions.Add(new(() => injections.MethodInjection(_varName, curCtx, curMethod, curVariables), curMethod.Ordinal));
        _varInjections.Clear();
    }

    public override void VisitProperty(in CodeContext ctx, in DpProperty property, int? position)
    {
        base.VisitProperty(in ctx, in property, position);
        var curCtx = ctx;
        var curProperty = property;
        var curVariable = _varInjections.Single();
        _actions.Add(new(() => injections.PropertyInjection(_varName, curCtx, curProperty, curVariable), curProperty.Ordinal));
        _varInjections.Clear();
    }

    public override void VisitField(in CodeContext ctx, in DpField field, int? position)
    {
        base.VisitField(in ctx, in field, position);
        var curCtx = ctx;
        var curField = field;
        var curVariable = _varInjections.Single();
        _actions.Add(new(() => injections.FieldInjection(_varName, curCtx, curField, curVariable), curField.Ordinal));
        _varInjections.Clear();
    }
}