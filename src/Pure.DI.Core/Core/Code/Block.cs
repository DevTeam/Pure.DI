namespace Pure.DI.Core.Code;

internal record Block(
    IStatement? Parent,
    LinkedList<IStatement> Statements) : IStatement
{
    private readonly Lazy<Variable> _current = new(() => Statements.OfType<Variable>().Last());

    public Variable Current => _current.Value;
}