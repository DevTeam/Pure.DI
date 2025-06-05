namespace Pure.DI.Core.Code;

using v2;

interface IBuildTools
{
    string NullCheck(Compilation compilation, string variableName);

    void AddPureHeader(LinesBuilder code);

    string GetDeclaration(Variable variable, string separator = " ");

    string OnInjected(BuildContext ctx, Variable variable);

    IEnumerable<Line> OnCreated(BuildContext ctx, Variable variable);

    void AddAggressiveInlining(LinesBuilder code);

    string GetDeclaration(CodeContext ctx, Var var, string separator = " ");

    IEnumerable<Line> OnCreated(CodeContext ctx, Var var);

    string OnInjected(CodeContext ctx, Var var);
}