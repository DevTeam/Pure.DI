using System.Runtime.CompilerServices;

namespace Pure.DI.Core;

sealed class Metadata(
    ISemantic semantic,
    ITypeSymbolComparer typeSymbolComparer)
    : IMetadata
{
    private const string IConfigurationTypeName = $"{Names.GeneratorName}.{nameof(IConfiguration)}";
    private readonly ConditionalWeakTable<Compilation, ITypeSymbol?> _configTypeSymbols = new();

    public bool IsMetadata(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        if (node is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        for (var curInvocation = invocation;;)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            var expression = curInvocation.Expression;
            switch (expression)
            {
                case IdentifierNameSyntax { Identifier.Text: nameof(DI.Setup) }:
                case MemberAccessExpressionSyntax { Name.Identifier.Text: nameof(DI.Setup) }
                    when expression.Kind() == SyntaxKind.SimpleMemberAccessExpression:
                    if (expression is MemberAccessExpressionSyntax
                        {
                            Expression: IdentifierNameSyntax { Identifier.Text: nameof(DI) }
                        })
                    {
                        return true;
                    }

                    var returnType = TryGetReturnType(semanticModel, node);
                    if (returnType is null)
                    {
                        return IsSetupSyntax(expression);
                    }

                    var configType = _configTypeSymbols.GetValue(
                        semanticModel.Compilation,
                        static compilation => compilation.GetTypeByMetadataName(IConfigurationTypeName));

                    return typeSymbolComparer.RuntimeEquals(returnType, configType);
            }

            if (expression is MemberAccessExpressionSyntax { Expression: InvocationExpressionSyntax innerInvocation })
            {
                curInvocation = innerInvocation;
                continue;
            }

            break;
        }

        return false;
    }

    private ITypeSymbol? TryGetReturnType(SemanticModel semanticModel, SyntaxNode node)
    {
        try
        {
            return semantic.TryGetTypeSymbol<ITypeSymbol>(semanticModel, node);
        }
        catch (HandledException)
        {
            return null;
        }
    }

    private static bool IsSetupSyntax(ExpressionSyntax expression) =>
        expression is IdentifierNameSyntax { Identifier.Text: nameof(DI.Setup) }
        || expression is MemberAccessExpressionSyntax {
            Name.Identifier.Text: nameof(DI.Setup),
            Expression: IdentifierNameSyntax { Identifier.Text: nameof(DI) }
        };
}
