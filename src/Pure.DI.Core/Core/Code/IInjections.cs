namespace Pure.DI.Core.Code;

internal interface IInjections
{
    void FieldInjection(string targetName, BuildContext ctx, DpField field, Variable fieldVariable);
    
    void PropertyInjection(string targetName, BuildContext ctx, DpProperty property, Variable propertyVariable);
    
    void MethodInjection(string targetName, BuildContext ctx, DpMethod method, ImmutableArray<Variable> methodArgs);
}