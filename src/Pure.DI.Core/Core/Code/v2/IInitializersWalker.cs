namespace Pure.DI.Core.Code.v2;

interface IV2InitializersWalker
{
    IV2InitializersWalker Ininitialize(Action<Var> buildVar, string variableName, IEnumerator<Var> vars);

    void VisitInitializer(in CodeContext ctx, DpInitializer initializer);
}