// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

using v2;

sealed class Injections(IBuildTools buildTools) : IInjections
{
    public void FieldInjection(string targetName, BuildContext ctx, DpField field, Variable fieldVariable) =>
        ctx.Code.AppendLine($"{targetName}.{field.Field.Name} = {ctx.BuildTools.OnInjected(ctx, fieldVariable)};");

    public void PropertyInjection(string targetName, BuildContext ctx, DpProperty property, Variable propertyVariable) =>
        ctx.Code.AppendLine($"{targetName}.{property.Property.Name} = {ctx.BuildTools.OnInjected(ctx, propertyVariable)};");

    public void MethodInjection(string targetName, BuildContext ctx, DpMethod method, IReadOnlyList<Variable> methodVariables)
    {
        var args = new List<string>();
        for (var index = 0; index < methodVariables.Count; index++)
        {
            var variable = methodVariables[index];
            if (index < method.Parameters.Length)
            {
                variable = variable with
                {
                    RefKind = method.Parameters[index].ParameterSymbol.RefKind
                };
            }

            args.Add(ctx.BuildTools.OnInjected(ctx, variable));
        }

        ctx.Code.AppendLine($"{targetName}.{method.Method.Name}({string.Join(", ", args)});");
    }

    public void FieldInjection(string targetName, CodeContext ctx, DpField field, Var fieldVar) =>
        ctx.Lines.AppendLine($"{targetName}.{field.Field.Name} = {buildTools.OnInjected(ctx, fieldVar)};");

    public void PropertyInjection(string targetName, CodeContext ctx, DpProperty property, Var propertyVar) =>
        ctx.Lines.AppendLine($"{targetName}.{property.Property.Name} = {buildTools.OnInjected(ctx, propertyVar)};");

    public void MethodInjection(string targetName, CodeContext ctx, DpMethod method, IReadOnlyList<Var> methodVars)
    {
        var args = new List<string>();
        for (var index = 0; index < methodVars.Count; index++)
        {
            var var = methodVars[index];
            if (index < method.Parameters.Length)
            {
                var.Declaration.RefKind = method.Parameters[index].ParameterSymbol.RefKind;
            }

            args.Add(buildTools.OnInjected(ctx, var));
        }

        ctx.Lines.AppendLine($"{targetName}.{method.Method.Name}({string.Join(", ", args)});");
    }
}