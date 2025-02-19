// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

sealed class Injections : IInjections
{
    public void FieldInjection(string targetName, BuildContext ctx, DpField field, Variable fieldVariable) =>
        ctx.Code.AppendLine($"{targetName}.{field.Field.Name} = {ctx.BuildTools.OnInjected(ctx, fieldVariable)};");

    public void PropertyInjection(string targetName, BuildContext ctx, DpProperty property, Variable propertyVariable) =>
        ctx.Code.AppendLine($"{targetName}.{property.Property.Name} = {ctx.BuildTools.OnInjected(ctx, propertyVariable)};");

    public void MethodInjection(string targetName, BuildContext ctx, DpMethod method, IReadOnlyList<Variable> methodArgs)
    {
        var args = new List<string>();
        for (var index = 0; index < methodArgs.Count; index++)
        {
            var variable = methodArgs[index];
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
}