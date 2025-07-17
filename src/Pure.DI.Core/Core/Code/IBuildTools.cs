namespace Pure.DI.Core.Code;

interface IBuildTools
{
    string NullCheck(Compilation compilation, string variableName);

    void AddPureHeader(LinesBuilder code);

    void AddAggressiveInlining(LinesBuilder code);

    string GetDeclaration(CodeContext ctx, VarDeclaration varDeclaration, string separator = " ", bool useVar = false);

    IEnumerable<Line> OnCreated(CodeContext ctx, VarInjection varInjection);

    string OnInjected(CodeContext ctx, VarInjection varInjection);
}