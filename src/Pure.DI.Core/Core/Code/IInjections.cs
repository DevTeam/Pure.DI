namespace Pure.DI.Core.Code;

using v2;

interface IInjections
{
    void FieldInjection(string targetName, BuildContext ctx, DpField field, Variable fieldVariable);

    void PropertyInjection(string targetName, BuildContext ctx, DpProperty property, Variable propertyVariable);

    void MethodInjection(string targetName, BuildContext ctx, DpMethod method, IReadOnlyList<Variable> methodVariables);

    void FieldInjection(string targetName, CodeContext ctx, DpField field, Var fieldVar);

    void PropertyInjection(string targetName, CodeContext ctx, DpProperty property, Var propertyVar);

    void MethodInjection(string targetName, CodeContext ctx, DpMethod method, IReadOnlyList<Var> methodVars);
}