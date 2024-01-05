namespace Pure.DI.Core.Code;

internal interface IBuildTools
{
    void AddPureHeader(LinesBuilder code);
    
    string GetDeclaration(Variable variable, bool typeIsRequired = false);

    string OnInjected(BuildContext ctx, Variable variable);
    
    IReadOnlyCollection<Line> OnCreated(BuildContext ctx, Variable variable);
}