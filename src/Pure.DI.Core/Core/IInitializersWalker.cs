namespace Pure.DI.Core;

interface IInitializersWalker
{
    IInitializersWalker Ininitialize(string variableName, IEnumerator<Variable> variables);

    void VisitInitializer(in BuildContext ctx, DpInitializer initializer);
}