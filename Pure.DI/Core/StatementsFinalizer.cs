// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal class StatementsFinalizer : IStatementsFinalizer
{
    private readonly IBuildContext _buildContext;

    public StatementsFinalizer(IBuildContext buildContext) => _buildContext = buildContext;

    public BlockSyntax? AddFinalizationStatements(BlockSyntax? block)
    {
        if (block == default || !_buildContext.FinalizationStatements.Any())
        {
            return block;
        }

        var memberKey = new MemberKey($"_deepness", typeof(StatementsFinalizer));
        var filedName = _buildContext.NameService.FindName(memberKey);
        _buildContext.GetOrAddMember(memberKey, () =>
            SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("int"))
                    .AddVariables(
                        SyntaxFactory.VariableDeclarator(filedName)
                    )
            )
            .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxRepo.ThreadStaticAttr))
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword)));

        var refToDeepness = SyntaxFactory.Argument(SyntaxFactory.IdentifierName(filedName)).WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.RefKeyword));
        var incrementStatement = SyntaxFactory.InvocationExpression(
            SyntaxFactory.IdentifierName("System.Threading.Interlocked.Increment"),
            SyntaxFactory.ArgumentList().AddArguments(refToDeepness));

        var decrementStatement = SyntaxFactory.InvocationExpression(
            SyntaxFactory.IdentifierName("System.Threading.Interlocked.Decrement"),
            SyntaxFactory.ArgumentList().AddArguments(refToDeepness));

        var releaseBlock = SyntaxFactory.Block()
            .AddStatements(
                SyntaxFactory.IfStatement(
                    SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, decrementStatement, SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))),
                    SyntaxFactory.Block().AddStatements(_buildContext.FinalizationStatements.ToArray())
                ));

        var tryStatement = SyntaxFactory.TryStatement(
            block,
            SyntaxFactory.List<CatchClauseSyntax>(),
            SyntaxFactory.FinallyClause(releaseBlock));
        
        return
            SyntaxFactory.Block()
                .AddStatements(SyntaxFactory.ExpressionStatement(incrementStatement))
                .AddStatements(tryStatement);
    }
}