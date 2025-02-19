namespace Pure.DI.Core.Models;

record Block(
    int Id,
    IStatement? Parent,
    LinkedList<IStatement> Statements) : IStatement
{
    public Variable Current => (Variable)Statements.Last.Value;
}