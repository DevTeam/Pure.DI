// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident
namespace Pure.DI.Core.Code;

sealed class InitializersWalker(
    IInjections injections,
    InitializersWalkerContext ctx)
    : IInitializersWalker
{
    private readonly List<(Action Run, int? Ordinal)> _actions = [];

    public IEnumerator VisitInitializer(CodeContext codeCtx, DpInitializer initializer)
    {
        _actions.Clear();
        foreach (var field in initializer.Fields.Where(_ => ctx.VarInjections.MoveNext()))
        {
            yield return ctx.BuildVarInjection(ctx.VarInjections.Current);
            var curVariable = ctx.VarInjections.Current!;
            var curField = field;
            var curCtx = codeCtx;
            _actions.Add((() => injections.FieldInjection(ctx.VariableName, curCtx, curField, curVariable), curField.Ordinal));
        }

        foreach (var property in initializer.Properties.Where(_ => ctx.VarInjections.MoveNext()))
        {
            yield return ctx.BuildVarInjection(ctx.VarInjections.Current);
            var curVariable = ctx.VarInjections.Current!;
            var curProperty = property;
            var curCtx = codeCtx;
            _actions.Add((() => injections.PropertyInjection(ctx.VariableName, curCtx, curProperty, curVariable), curProperty.Ordinal));
        }

        foreach (var method in initializer.Methods)
        {
            var curVariables = new List<VarInjection>();
            foreach (var unused in method.Parameters.Where(_ => ctx.VarInjections.MoveNext()))
            {
                yield return ctx.BuildVarInjection(ctx.VarInjections.Current);
                curVariables.Add(ctx.VarInjections.Current);
            }

            var curMethod = method;
            var curCtx = codeCtx;
            _actions.Add((() => injections.MethodInjection(ctx.VariableName, curCtx, curMethod, curVariables), curMethod.Ordinal));
        }

        foreach (var action in _actions.OrderBy(i => i.Ordinal ?? int.MaxValue).Select(i => i.Run))
        {
            action();
        }
    }
}