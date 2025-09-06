namespace Pure.DI.Core.Code;

interface IBuildTools
{
    string NullCheck(Compilation compilation, string variableName);

    void AddPureHeader(Lines code);

    void AddAggressiveInlining(Lines code);

    void AddNoInlining(Lines code);

    string GetDeclaration(CodeContext ctx, VarDeclaration varDeclaration, string separator = " ", bool useVar = false);

    Lines OnCreated(CodeContext ctx, VarInjection varInjection);

    string OnInjected(CodeContext ctx, VarInjection varInjection);
}