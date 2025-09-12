// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

sealed class Injections(IBuildTools buildTools) : IInjections
{
    public void FieldInjection(string targetName, CodeContext ctx, DpField field, VarInjection fieldVarInjection) =>
        ctx.Lines.AppendLine($"{targetName}.{field.Field.Name} = {buildTools.OnInjected(ctx, fieldVarInjection)};");

    public void PropertyInjection(string targetName, CodeContext ctx, DpProperty property, VarInjection propertyVarInjection) =>
        ctx.Lines.AppendLine($"{targetName}.{property.Property.Name} = {buildTools.OnInjected(ctx, propertyVarInjection)};");

    public void MethodInjection(string targetName, CodeContext ctx, DpMethod method, IReadOnlyList<VarInjection> methodArgsVarInjections)
    {
        var args = methodArgsVarInjections.Select(methodArgVarInjection => buildTools.OnInjected(ctx, methodArgVarInjection));
        ctx.Lines.AppendLine($"{targetName}.{method.Method.Name}({string.Join(", ", args)});");
    }
}