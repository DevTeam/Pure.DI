namespace Pure.DI.Core.Code;

internal interface IFactoryValidator
{
    IFactoryValidator Initialize(DpFactory factory);

    void Visit(SyntaxNode? node);
}