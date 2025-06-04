namespace Pure.DI.Core.Code;

interface IInitializersWalker
{
    IInitializersWalker Ininitialize(Action<VarInjection> buildVarInjection, string variableName, IEnumerator<VarInjection> varInjections);

    void VisitInitializer(in CodeContext ctx, DpInitializer initializer);
}