namespace Pure.DI.Core;

internal interface IStatementsBlockWrapper
{
    BlockSyntax? AddFinalizationStatements(BlockSyntax? block);
}