namespace Pure.DI.Core.Code;

internal record Block(
    IStatement? Parent,
    LinkedList<IStatement> Statements) : IStatement
{
    public Variable Current => Statements.OfType<Variable>().Last();
}