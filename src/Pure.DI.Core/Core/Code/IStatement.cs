namespace Pure.DI.Core.Code;

interface IStatement
{
    IStatement? Parent { get; }

    Variable Current { get; }
}