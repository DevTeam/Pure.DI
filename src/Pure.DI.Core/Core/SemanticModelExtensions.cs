namespace Pure.DI.Core;

using Microsoft.CodeAnalysis.Operations;

internal static class SemanticModelExtensions
{
    public static T? TryGetTypeSymbol<T>(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        where T : ITypeSymbol
    {
        var typeInfo = semanticModel.GetTypeInfo(node, cancellationToken);
        var typeSymbol = typeInfo.Type ?? typeInfo.ConvertedType;
        if (typeSymbol is T symbol)
        {
            return (T)symbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
        }

        return default;
    }
    
    public static T GetTypeSymbol<T>(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        where T : ITypeSymbol
    {
        var result = TryGetTypeSymbol<T>(semanticModel, node, cancellationToken);
        if (result is not null)
        {
            return result;
        }

        throw new CompileErrorException($"The type {node} is not supported.", node.GetLocation(), LogId.ErrorInvalidMetadata);
    }
    
    public static T GetRequiredConstantValue<T>(this SemanticModel semanticModel, SyntaxNode node)
    {
        var value = GetConstantValue<T>(semanticModel, node);
        if (value is not null)
        {
            return value;
        }
        
        throw new CompileErrorException($"{node} must be a non-null value of type {typeof(T)}.", node.GetLocation(), LogId.ErrorInvalidMetadata);
    }

    public static T? GetConstantValue<T>(this SemanticModel semanticModel, SyntaxNode node)
    {
        switch (node)
        {
            case LiteralExpressionSyntax literalExpression:
            {
                if (literalExpression.IsKind(SyntaxKind.DefaultLiteralExpression)
                    || literalExpression.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    return default;
                }

                return (T?)literalExpression.Token.Value;
            }

            case MemberAccessExpressionSyntax memberAccessExpressionSyntax
                when memberAccessExpressionSyntax.IsKind(SyntaxKind.SimpleMemberAccessExpression):
            {
                if (memberAccessExpressionSyntax.Expression is IdentifierNameSyntax classIdentifierName)
                {
                    var enumValueStr = memberAccessExpressionSyntax.Name.Identifier.Text;
                    switch (classIdentifierName.Identifier.Text)
                    {
                        case nameof(CompositionKind):
                            if (Enum.TryParse<CompositionKind>(enumValueStr, out var compositionKindValue))
                            {
                                return (T)(object)compositionKindValue;
                            }

                            break;

                        case nameof(Lifetime):
                            if (Enum.TryParse<Lifetime>(enumValueStr, out var lifetimeValue))
                            {
                                return (T)(object)lifetimeValue;
                            }

                            break;

                        case nameof(Tag):
                            if (Enum.TryParse<Tag>(enumValueStr, out var tagValue))
                            {
                                return (T)(object)tagValue;
                            }

                            break;
                    }
                }
                
                break;
            }
        }
        
        var optionalValue = semanticModel.GetConstantValue(node);
        if (optionalValue.Value is not null)
        {
            return (T)optionalValue.Value;
        }

        var operation = semanticModel.GetOperation(node);
        if (operation?.ConstantValue.Value is not null)
        {
            return (T)operation.ConstantValue.Value!;
        }
        
        if (typeof(T) == typeof(object) && operation is ITypeOfOperation typeOfOperation)
        {
            return (T)typeOfOperation.TypeOperand;
        }

        throw new CompileErrorException($"{node} must be a constant value of type {typeof(T)}.", node.GetLocation(), LogId.ErrorInvalidMetadata);
    }
}