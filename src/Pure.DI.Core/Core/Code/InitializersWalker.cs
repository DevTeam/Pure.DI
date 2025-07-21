// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident
namespace Pure.DI.Core.Code;

sealed class InitializersWalker(
    IInjections injections,
    ILocationProvider locationProvider,
    InitializersWalkerContext ctx)
    : DependenciesWalker<CodeContext>(locationProvider), IInitializersWalker
{
    private readonly List<(Action Run, int? Ordinal)> _actions = [];
    private readonly List<VarInjection> _varInjections = [];

    public override void VisitInitializer(in CodeContext codeCtx, DpInitializer initializer)
    {
        base.VisitInitializer(in codeCtx, initializer);
        foreach (var action in _actions.OrderBy(i => i.Ordinal ?? int.MaxValue).Select(i => i.Run))
        {
            action();
        }
    }

    public override void VisitInjection(in CodeContext codeCtx, in Injection injection, bool hasExplicitDefaultValue, object? explicitDefaultValue, in ImmutableArray<Location> locations, int? position)
    {
        if (ctx.VarInjections.MoveNext())
        {
            ctx.BuildVarInjection.Invoke(ctx.VarInjections.Current);
            _varInjections.Add(ctx.VarInjections.Current);
        }

        base.VisitInjection(in codeCtx, in injection, hasExplicitDefaultValue, explicitDefaultValue, in locations, position);
    }

    public override void VisitMethod(in CodeContext codeCtx, in DpMethod method, int? position)
    {
        base.VisitMethod(in codeCtx, in method, position);
        var curCtx = codeCtx;
        var curMethod = method;
        var curVariables = _varInjections.ToList();
        _actions.Add(new(() => injections.MethodInjection(ctx.VariableName, curCtx, curMethod, curVariables), curMethod.Ordinal));
        _varInjections.Clear();
    }

    public override void VisitProperty(in CodeContext codeCtx, in DpProperty property, int? position)
    {
        base.VisitProperty(in codeCtx, in property, position);
        var curCtx = codeCtx;
        var curProperty = property;
        var curVariable = _varInjections.Single();
        _actions.Add(new(() => injections.PropertyInjection(ctx.VariableName, curCtx, curProperty, curVariable), curProperty.Ordinal));
        _varInjections.Clear();
    }

    public override void VisitField(in CodeContext codeCtx, in DpField field, int? position)
    {
        base.VisitField(in codeCtx, in field, position);
        var curCtx = codeCtx;
        var curField = field;
        var curVariable = _varInjections.Single();
        _actions.Add(new(() => injections.FieldInjection(ctx.VariableName, curCtx, curField, curVariable), curField.Ordinal));
        _varInjections.Clear();
    }
}