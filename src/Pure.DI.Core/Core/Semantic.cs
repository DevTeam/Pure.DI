// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

using Microsoft.CodeAnalysis.Operations;

internal sealed class Semantic(
    IInjectionSiteFactory injectionSiteFactory,
    IWildcardMatcher wildcardMatcher,
    ITypes types,
    ISmartTags smartTags,
    CancellationToken cancellationToken)
    : ISemantic
{
    public bool IsAccessible(ISymbol symbol) =>
        symbol is { IsStatic: false, DeclaredAccessibility: Accessibility.Internal or Accessibility.Public };

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
                        case nameof(CompositionKind) when IsSpecialType(semanticModel, node, SpecialType.CompositionKind):
                            if (Enum.TryParse<CompositionKind>(valueStr, out var compositionKindValue))
                            {
                                return (T)(object)compositionKindValue;
                            }

                            break;

                        case nameof(Lifetime) when IsSpecialType(semanticModel, node, SpecialType.Lifetime):
                            if (Enum.TryParse<Lifetime>(valueStr, out var lifetimeValue))
                            {
                                return (T)(object)lifetimeValue;
                            }

                            break;

                        case nameof(Tag) when typeof(T) == typeof(object):
                            return valueStr switch
                            {
                                nameof(Tag.Type) when IsSpecialType(semanticModel, node, SpecialType.Tag) => (T)(object)Tag.Type,
                                nameof(Tag.Unique) when IsSpecialType(semanticModel, node, SpecialType.Tag) => (T)(object)Tag.Unique,
                                _ => (T)smartTags.Register(valueStr)
                            };
                    }
                }

                break;
            }
            
            case IdentifierNameSyntax identifierNameSyntax when typeof(T) == typeof(object):
                return identifierNameSyntax.Identifier.Text switch
                {
                    nameof(Tag.Type) when IsSpecialType(semanticModel, node, SpecialType.Tag) => (T)(object)Tag.Type,
                    nameof(Tag.Unique) when IsSpecialType(semanticModel, node, SpecialType.Tag) => (T)(object)Tag.Unique,
                    _ => (T)smartTags.Register(identifierNameSyntax.Identifier.Text)
                };

            case InvocationExpressionSyntax invocationExpressionSyntax:
            {
                switch (invocationExpressionSyntax.Expression)
                {
                    case MemberAccessExpressionSyntax { Name.Identifier.Text: nameof(Tag.On), Expression: IdentifierNameSyntax { Identifier.Text: nameof(Tag) } }:
                        if (invocationExpressionSyntax.ArgumentList.Arguments is var injectionSitesArgs)
                        {
                            var injectionSites = injectionSitesArgs
                                .Select(injectionSiteArg => (Source: injectionSiteArg.Expression, Value: GetConstantValue<string>(semanticModel, injectionSiteArg.Expression)))
                                .Where(i => !string.IsNullOrWhiteSpace(i.Value))
                                .Select(i => new MdInjectionSite(i.Source, i.Value!))
                                .ToImmutableArray();

                            return (T)MdTag.CreateTagOnValue(invocationExpressionSyntax, injectionSites);
                        }

                        // ReSharper disable once HeuristicUnreachableCode
                        break;

                    case MemberAccessExpressionSyntax { Name: GenericNameSyntax { TypeArgumentList.Arguments: [{ } typeArg] }, Name.Identifier.Text: nameof(Tag.OnConstructorArg), Expression: IdentifierNameSyntax { Identifier.Text: nameof(Tag) } }:
                        if (invocationExpressionSyntax.ArgumentList.Arguments is [{ } ctorArgName])
                        {
                            var name = GetRequiredConstantValue<string>(semanticModel, ctorArgName.Expression);
                            var ctor = GetTypeSymbol<ITypeSymbol>(semanticModel, typeArg)
                                .GetMembers()
                                .OfType<IMethodSymbol>()
                                .FirstOrDefault(i =>
                                    IsAccessible(i)
                                    && !i.IsStatic
                                    && i.Parameters.Any(p => wildcardMatcher.Match(name.AsSpan(), p.Name.AsSpan())));

                            if (ctor is null)
                            {
                                throw new CompileErrorException($"There is no accessible non-static constructor of type {typeArg} with an argument matching \"{name}\".", invocationExpressionSyntax.GetLocation(), LogId.ErrorInvalidMetadata);
                            }

                            var injectionSite = injectionSiteFactory.CreateInjectionSite(ctorArgName.Expression, ctor, name);
                            return (T)MdTag.CreateTagOnValue(invocationExpressionSyntax, ImmutableArray.Create(injectionSite));
                        }

                        break;

                    case MemberAccessExpressionSyntax { Name: GenericNameSyntax { TypeArgumentList.Arguments: [{ } typeArg] }, Name.Identifier.Text: nameof(Tag.OnMethodArg), Expression: IdentifierNameSyntax { Identifier.Text: nameof(Tag) } }:
                        if (invocationExpressionSyntax.ArgumentList.Arguments is [{ } methodNameArg, { } methodArgName])
                        {
                            var methodName = GetRequiredConstantValue<string>(semanticModel, methodNameArg.Expression);
                            var methodArg = GetRequiredConstantValue<string>(semanticModel, methodArgName.Expression);
                            var method = GetTypeSymbol<ITypeSymbol>(semanticModel, typeArg)
                                .GetMembers()
                                .OfType<IMethodSymbol>()
                                .FirstOrDefault(i =>
                                    i.MethodKind == MethodKind.Ordinary
                                    && IsAccessible(i)
                                    && wildcardMatcher.Match(methodName.AsSpan(), i.Name.AsSpan())
                                    && i.Parameters.Any(p => wildcardMatcher.Match(methodArg.AsSpan(), p.Name.AsSpan())));

                            if (method is null)
                            {
                                throw new CompileErrorException($"There is no accessible non-static method of type {typeArg} with a name matching \"{methodName}\" an argument matching \"{methodArg}\".", invocationExpressionSyntax.GetLocation(), LogId.ErrorInvalidMetadata);
                            }

                            var injectionSite = injectionSiteFactory.CreateInjectionSite(methodArgName.Expression, method, methodArg);
                            return (T)MdTag.CreateTagOnValue(invocationExpressionSyntax, ImmutableArray.Create(injectionSite));
                        }

                        break;

                    case MemberAccessExpressionSyntax { Name: GenericNameSyntax { TypeArgumentList.Arguments: [{ } typeArg] }, Name.Identifier.Text: nameof(Tag.OnMember), Expression: IdentifierNameSyntax { Identifier.Text: nameof(Tag) } }:
                        if (invocationExpressionSyntax.ArgumentList.Arguments is [{ } memberNameArg])
                        {
                            var name = GetRequiredConstantValue<string>(semanticModel, memberNameArg.Expression);
                            var type = GetTypeSymbol<ITypeSymbol>(semanticModel, typeArg);
                            var member = type
                                .GetMembers()
                                .FirstOrDefault(i =>
                                    IsAccessible(i)
                                    && i is IFieldSymbol { IsReadOnly: false, IsConst: false } or IPropertySymbol { IsReadOnly: false, SetMethod: not null }
                                    && wildcardMatcher.Match(name.AsSpan(), i.Name.AsSpan()));

                            if (member is null)
                            {
                                throw new CompileErrorException($"There is no accessible non-static writable field or property matched with \"{name}\" of {typeArg}.", invocationExpressionSyntax.GetLocation(), LogId.ErrorInvalidMetadata);
                            }

                            var injectionSite = injectionSiteFactory.CreateInjectionSite(memberNameArg, type, name);
                            return (T)MdTag.CreateTagOnValue(invocationExpressionSyntax, ImmutableArray.Create(injectionSite));
                        }

                        break;
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

    private bool IsSpecialType(SemanticModel semanticModel, SyntaxNode node, SpecialType specialType) =>
        semanticModel.GetTypeInfo(node).Type is { } type
        && types.TypeEquals(type, types.TryGet(specialType, semanticModel.Compilation));

    public bool IsValidNamespace(INamespaceSymbol? namespaceSymbol) => 
        namespaceSymbol is { IsImplicitlyDeclared: false };
}