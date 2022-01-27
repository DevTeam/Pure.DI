namespace Pure.DI.Core;

using Microsoft.CodeAnalysis.CSharp.Syntax;

internal interface IStatementsFinalizer
{
    BlockSyntax? AddFinalizationStatements(BlockSyntax? block);
}