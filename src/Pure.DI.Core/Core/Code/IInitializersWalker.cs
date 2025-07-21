namespace Pure.DI.Core.Code;

interface IInitializersWalker
{
    void VisitInitializer(in CodeContext ctx, DpInitializer initializer);
}