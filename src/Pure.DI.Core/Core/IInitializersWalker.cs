namespace Pure.DI.Core;

internal interface IInitializersWalker
{
    IInitializersWalker Ininitialize(string variableName, IEnumerator<Variable> variables);

    void VisitInitializer(in BuildContext ctx, DpInitializer initializer);
}