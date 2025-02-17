namespace Pure.DI.Core;

interface IConstructorInjectionsCounterWalker
{
    int Count { get; }
    void VisitConstructor(in Unit ctx, in DpMethod constructor);
}