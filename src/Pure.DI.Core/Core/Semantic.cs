// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using Microsoft.CodeAnalysis.Operations;

internal class Semantic(CancellationToken cancellationToken): ISemantic
{
    public bool IsAccessible(ISymbol symbol) => 
        symbol is { IsStatic: false, DeclaredAccessibility: Accessibility.Internal or Accessibility.Public or Accessibility.Friend };

    public T? TryGetTypeSymbol<T>(SemanticModel semanticModel, SyntaxNode node)
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
    
    public T GetTypeSymbol<T>(SemanticModel semanticModel, SyntaxNode node)
        where T : ITypeSymbol
    {
        var result = TryGetTypeSymbol<T>(semanticModel, node);
        if (result is not null)
        {
            return result;
        }

        throw new CompileErrorException($"The type {node} is not supported.", node.GetLocation(), LogId.ErrorInvalidMetadata);
    }
    
    public T GetRequiredConstantValue<T>(SemanticModel semanticModel, SyntaxNode node)
    {
        var value = GetConstantValue<T>(semanticModel, node);
        if (value is not null)
        {
            return value;
        }
        
        throw new CompileErrorException($"{node} must be a non-null value of type {typeof(T)}.", node.GetLocation(), LogId.ErrorInvalidMetadata);
    }

    public T?[] GetConstantValues<T>(SemanticModel semanticModel, SyntaxNode node)
    {
#if ROSLYN4_8_OR_GREATER
        if (node is CollectionExpressionSyntax collectionExpression)
        {
            return collectionExpression.Elements
                    .SelectMany(e => e.ChildNodes())
                    .Select(e => GetConstantValue<T>(semanticModel, e))
                    .ToArray();
        }
#endif        

        return [GetConstantValue<T>(semanticModel, node)];
    }

    public T? GetConstantValue<T>(SemanticModel semanticModel, SyntaxNode node)
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
                    var valueStr = memberAccessExpressionSyntax.Name.Identifier.Text;
                    switch (classIdentifierName.Identifier.Text)
                    {
                        case nameof(CompositionKind):
                            if (Enum.TryParse<CompositionKind>(valueStr, out var compositionKindValue))
                            {
                                return (T)(object)compositionKindValue;
                            }

                            break;

                        case nameof(Lifetime):
                            if (Enum.TryParse<Lifetime>(valueStr, out var lifetimeValue))
                            {
                                return (T)(object)lifetimeValue;
                            }

                            break;

                        case nameof(Tag):
                            switch (valueStr)
                            {
                                case nameof(Tag.Type):
                                    return (T)(object)Tag.Type; 
                                
                                case nameof(Tag.Unique):
                                    return (T)(object)Tag.Unique;
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

        throw new CompileErrorException($"{node} must be a constant value of type {typeof(T)} or a special API call.", node.GetLocation(), LogId.ErrorInvalidMetadata);
    }
}