namespace Pure.DI.Core;

internal sealed class InitializersWalker(
    string variableName,
    IEnumerator<Variable> variables,
    IInjections injections):  DependenciesWalker<BuildContext>
{
    private readonly List<Variable> _variables = new();
    
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
        injections.MethodInjection(variableName, ctx, method, _variables.ToImmutableArray());
        _variables.Clear();
    }

    public override void VisitProperty(in BuildContext ctx, in DpProperty property)
    {
        base.VisitProperty(in ctx, in property);
        injections.PropertyInjection(variableName, ctx, property, _variables.Single());
        _variables.Clear();
    }

    public override void VisitField(in BuildContext ctx, in DpField field)
    {
        base.VisitField(in ctx, in field);
        injections.FieldInjection(variableName, ctx, field, _variables.Single());
        _variables.Clear();
    }
}