namespace Pure.DI.Core.Code;

interface IFactoryValidator
{
    IFactoryValidator Initialize(DpFactory factory);

    void Visit(SyntaxNode? node);
}