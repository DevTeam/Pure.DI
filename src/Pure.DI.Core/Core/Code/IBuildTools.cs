namespace Pure.DI.Core.Code;

internal interface IBuildTools
{
    string GetDeclaration(Variable variable, bool typeIsRequired = false);

    string OnInjected(BuildContext ctx, Variable variable);
    
    IEnumerable<Line> OnCreated(BuildContext ctx, Variable variable);
}