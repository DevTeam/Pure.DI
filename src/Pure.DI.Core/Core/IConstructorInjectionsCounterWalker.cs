namespace Pure.DI.Core;

internal interface IConstructorInjectionsCounterWalker
{
    int Count { get; }
    void VisitConstructor(in Unit ctx, in DpMethod constructor);
}