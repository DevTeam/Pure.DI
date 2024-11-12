// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident
namespace Pure.DI.Core;

internal sealed class InitializersWalker(
    string variableName,
    IEnumerator<Variable> variables,
    IInjections injections):  DependenciesWalker<BuildContext>
{
    private readonly List<Variable> _variables = [];
    private readonly List<(Action Run, int? Ordinal)> _actions = [];

    public override void VisitInitializer(in BuildContext ctx, DpInitializer initializer)
    {
        base.VisitInitializer(in ctx, initializer);
        foreach (var action in _actions.OrderBy(i => i.Ordinal ?? int.MaxValue).Select(i => i.Run))
        {
            action();
        }
    }

    public override void VisitInjection(in BuildContext ctx, in Injection injection, bool hasExplicitDefaultValue, object? explicitDefaultValue, in ImmutableArray<Location> locations)
    {
        if (variables.MoveNext())
        {
            _variables.Add(variables.Current);
        }

        base.VisitInjection(in ctx, in injection, hasExplicitDefaultValue, explicitDefaultValue, in locations);
    }

    public override void VisitMethod(in BuildContext ctx, in DpMethod method)
    {
        base.VisitMethod(in ctx, in method);
        var curCtx = ctx;
        var curMethod = method;
        var curVariables = _variables.ToImmutableArray();
        _actions.Add(new (() => injections.MethodInjection(variableName, curCtx, curMethod, curVariables), curMethod.Ordinal));
        _variables.Clear();
    }

    public override void VisitProperty(in BuildContext ctx, in DpProperty property)
    {
        base.VisitProperty(in ctx, in property);
        var curCtx = ctx;
        var curProperty = property;
        var curVariable = _variables.Single();
        _actions.Add(new (() => injections.PropertyInjection(variableName, curCtx, curProperty, curVariable), curProperty.Ordinal));
        _variables.Clear();
    }

    public override void VisitField(in BuildContext ctx, in DpField field)
    {
        base.VisitField(in ctx, in field);
        var curCtx = ctx;
        var curField = field;
        var curVariable = _variables.Single();
        _actions.Add(new (() => injections.FieldInjection(variableName, curCtx, curField, curVariable), curField.Ordinal));
        _variables.Clear();
    }
}