// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

sealed class Injections(IBuildTools buildTools) : IInjections
{
    public void FieldInjection(string targetName, CodeContext ctx, DpField field, VarInjection fieldVarInjection) =>
        ctx.Lines.AppendLine($"{targetName}.{field.Field.Name} = {buildTools.OnInjected(ctx, fieldVarInjection)};");

    public void PropertyInjection(string targetName, CodeContext ctx, DpProperty property, VarInjection propertyVarInjection) =>
        ctx.Lines.AppendLine($"{targetName}.{property.Property.Name} = {buildTools.OnInjected(ctx, propertyVarInjection)};");

    public void MethodInjection(string targetName, CodeContext ctx, DpMethod method, IReadOnlyList<VarInjection> methodVarInjections)
    {
        var args = new List<string>();
        for (var index = 0; index < methodVarInjections.Count; index++)
        {
            var varInjection = methodVarInjections[index];
            if (index < method.Parameters.Length)
            {
                varInjection.Var.Declaration.RefKind = method.Parameters[index].ParameterSymbol.RefKind;
            }

            args.Add(buildTools.OnInjected(ctx, varInjection));
        }

        ctx.Lines.AppendLine($"{targetName}.{method.Method.Name}({string.Join(", ", args)});");
    }
}