using System.Runtime.CompilerServices;

namespace Pure.DI.Core;

sealed class Metadata(ISemantic semantic)
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
                    var returnType = semantic.TryGetTypeSymbol<ITypeSymbol>(semanticModel, node);
                    if (returnType is null)
                    {
                        return false;
                    }

                    var configType = _configTypeSymbols.GetValue(
                        semanticModel.Compilation,
                        static compilation => compilation.GetTypeByMetadataName(IConfigurationTypeName));

                    return configType is not null
                           && SymbolEqualityComparer.Default.Equals(returnType, configType);
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
}
