namespace Pure.DI.Core.Code;

internal interface IBuildTools
{
    string NullCheck(Compilation compilation, string variableName);
    
    void AddPureHeader(LinesBuilder code);

    string GetDeclaration(Variable variable, string separator = " ");

    string OnInjected(BuildContext ctx, Variable variable);

    IEnumerable<Line> OnCreated(BuildContext ctx, Variable variable);

    void AddAggressiveInlining(LinesBuilder code);
}