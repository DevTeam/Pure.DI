namespace Pure.DI.Core.Code;

interface IInjections
{
    void FieldInjection(string targetName, CodeContext ctx, DpField field, VarInjection fieldVarInjection);

    void PropertyInjection(string targetName, CodeContext ctx, DpProperty property, VarInjection propertyVarInjection);

    void MethodInjection(string targetName, CodeContext ctx, DpMethod method, IReadOnlyList<VarInjection> methodVarInjections);
}