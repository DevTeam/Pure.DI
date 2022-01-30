namespace Pure.DI.Core;

internal interface IStatementsFinalizer
{
    BlockSyntax? AddFinalizationStatements(BlockSyntax? block);
}