// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident
namespace Pure.DI.Core.Code.v2;

sealed class V2InitializersWalker(
    IInjections injections, ILocationProvider locationProvider)
    : DependenciesWalker<CodeContext>(locationProvider), IV2InitializersWalker
{
    private readonly List<(Action Run, int? Ordinal)> _actions = [];
    private readonly List<Var> _vars = [];
    private Action<Var>? _buildVar;
    private string _varName = string.Empty;
    private IEnumerator<Var> _varsEnumerator = Enumerable.Empty<Var>().GetEnumerator();

    public IV2InitializersWalker Ininitialize(Action<Var> buildVar, string variableName, IEnumerator<Var> vars)
    {
        _buildVar = buildVar;
        _varName = variableName;
        _varsEnumerator = vars;
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
        if (_varsEnumerator.MoveNext())
        {
            _buildVar?.Invoke(_varsEnumerator.Current);
            _vars.Add(_varsEnumerator.Current);
        }

        base.VisitInjection(in ctx, in injection, hasExplicitDefaultValue, explicitDefaultValue, in locations, position);
    }

    public override void VisitMethod(in CodeContext ctx, in DpMethod method, int? position)
    {
        base.VisitMethod(in ctx, in method, position);
        var curCtx = ctx;
        var curMethod = method;
        var curVariables = _vars.ToList();
        _actions.Add(new(() => injections.MethodInjection(_varName, curCtx, curMethod, curVariables), curMethod.Ordinal));
        _vars.Clear();
    }

    public override void VisitProperty(in CodeContext ctx, in DpProperty property, int? position)
    {
        base.VisitProperty(in ctx, in property, position);
        var curCtx = ctx;
        var curProperty = property;
        var curVariable = _vars.Single();
        _actions.Add(new(() => injections.PropertyInjection(_varName, curCtx, curProperty, curVariable), curProperty.Ordinal));
        _vars.Clear();
    }

    public override void VisitField(in CodeContext ctx, in DpField field, int? position)
    {
        base.VisitField(in ctx, in field, position);
        var curCtx = ctx;
        var curField = field;
        var curVariable = _vars.Single();
        _actions.Add(new(() => injections.FieldInjection(_varName, curCtx, curField, curVariable), curField.Ordinal));
        _vars.Clear();
    }
}