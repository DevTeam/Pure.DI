namespace Pure.DI.Core.Code;

internal interface IStatement
{
    IStatement? Parent { get; }

    Variable Current { get; }
}