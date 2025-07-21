namespace Pure.DI.Core.Code;

interface IFactoryValidator
{
    void Visit(SyntaxNode? node);
}