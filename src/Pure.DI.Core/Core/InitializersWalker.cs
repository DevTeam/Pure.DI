// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident
namespace Pure.DI.Core;

sealed class InitializersWalker(IInjections injections)
    : DependenciesWalker<BuildContext>, IInitializersWalker
{
    private readonly List<(Action Run, int? Ordinal)> _actions = [];
    private readonly List<Variable> _variables = [];
    private string _variableName = string.Empty;
    private IEnumerator<Variable> _variablesEnumerator = Enumerable.Empty<Variable>().GetEnumerator();

    public IInitializersWalker Ininitialize(string variableName, IEnumerator<Variable> variables)
    {
        _variableName = variableName;
        _variablesEnumerator = variables;
        return this;
    }

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
        if (_variablesEnumerator.MoveNext())
        {
            _variables.Add(_variablesEnumerator.Current);
        }

        base.VisitInjection(in ctx, in injection, hasExplicitDefaultValue, explicitDefaultValue, in locations);
    }

    public override void VisitMethod(in BuildContext ctx, in DpMethod method)
    {
        base.VisitMethod(in ctx, in method);
        var curCtx = ctx;
        var curMethod = method;
        var curVariables = _variables.ToList();
        _actions.Add(new(() => injections.MethodInjection(_variableName, curCtx, curMethod, curVariables), curMethod.Ordinal));
        _variables.Clear();
    }

    public override void VisitProperty(in BuildContext ctx, in DpProperty property)
    {
        base.VisitProperty(in ctx, in property);
        var curCtx = ctx;
        var curProperty = property;
        var curVariable = _variables.Single();
        _actions.Add(new(() => injections.PropertyInjection(_variableName, curCtx, curProperty, curVariable), curProperty.Ordinal));
        _variables.Clear();
    }

    public override void VisitField(in BuildContext ctx, in DpField field)
    {
        base.VisitField(in ctx, in field);
        var curCtx = ctx;
        var curField = field;
        var curVariable = _variables.Single();
        _actions.Add(new(() => injections.FieldInjection(_variableName, curCtx, curField, curVariable), curField.Ordinal));
        _variables.Clear();
    }
}