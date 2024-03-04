namespace Pure.DI.Core.Code;

internal record Block(
    int Id,
    IStatement? Parent,
    LinkedList<IStatement> Statements) : IStatement
{
    public Variable Current => (Variable)Statements.Last.Value;
}