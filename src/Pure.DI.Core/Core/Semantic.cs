// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Operations;

sealed class Semantic(
    IInjectionSiteFactory injectionSiteFactory,
    IWildcardMatcher wildcardMatcher,
    ISmartTags smartTags,
    ILocationProvider locationProvider,
    CancellationToken cancellationToken)
    : ISemantic
{
    private readonly ConditionalWeakTable<SemanticModel, TypeSymbolCache> _typeSymbolCaches = new();

    public bool IsAccessible(ISymbol symbol) =>
        symbol is { IsStatic: false, DeclaredAccessibility: Accessibility.Internal or Accessibility.Public };

    public T? TryGetTypeSymbol<T>(SemanticModel semanticModel, SyntaxNode node)
        where T : ITypeSymbol
    {
        var typeSymbol = GetTypeSymbolCore(semanticModel, node);

        if (typeSymbol is not T symbol)
        {
            return default;
        }

        if (symbol is IErrorTypeSymbol)
        {
            throw HandledException.Shared;
        }

        return (T)symbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
    }

    public T GetTypeSymbol<T>(SemanticModel semanticModel, SyntaxNode node)
        where T : ITypeSymbol
    {
        var result = TryGetTypeSymbol<T>(semanticModel, node);
        if (result is not null)
        {
            return result;
        }

        throw new CompileErrorException(
            string.Format(Strings.Error_Template_NotSupported, node),
            ImmutableArray.Create(locationProvider.GetLocation(node)),
            LogId.ErrorNotSupportedSyntax,
            nameof(Strings.Error_Template_NotSupported));
    }

    public T GetRequiredConstantValue<T>(SemanticModel semanticModel, SyntaxNode node, SmartTagKind smartTagKind)
    {
        var value = GetConstantValue<T>(semanticModel, node, smartTagKind);
        if (value is not null)
        {
            return value;
        }

        throw new CompileErrorException(
            string.Format(Strings.Error_Template_MustBeValueOfType, node, typeof(T)),
            ImmutableArray.Create(locationProvider.GetLocation(node)),
            LogId.ErrorMustBeValueOfType,
            nameof(Strings.Error_Template_MustBeValueOfType));
    }

    public T?[] GetConstantValues<T>(SemanticModel semanticModel, SyntaxNode node, SmartTagKind smartTagKind)
    {
#if ROSLYN4_8_OR_GREATER
        if (node is CollectionExpressionSyntax collectionExpression)
        {
            return collectionExpression.Elements
                .SelectMany(e => e.ChildNodes())
                .Select(e => GetConstantValue<T>(semanticModel, e, smartTagKind))
                .ToArray();
        }
#endif

        return [GetConstantValue<T>(semanticModel, node, smartTagKind)];
    }

    public T? GetConstantValue<T>(SemanticModel semanticModel, SyntaxNode node, SmartTagKind smartTagKind)
    {
        switch (node)
        {
            // nameof()
            case InvocationExpressionSyntax {
                    Expression: IdentifierNameSyntax { Identifier.Text: "nameof" },
                    ArgumentList.Arguments: [{ Expression: IdentifierNameSyntax { Identifier.Text: {} name } }]
                }
                when typeof(T).IsAssignableFrom(typeof(string)):
                return (T)(object)name;

            // Literal expressions (no semantic model needed)
            case LiteralExpressionSyntax literalExpression
                when literalExpression.IsKind(SyntaxKind.DefaultLiteralExpression)
                     || literalExpression.IsKind(SyntaxKind.NullLiteralExpression):
                return default;

            case LiteralExpressionSyntax { Token.Value: T value }:
                return value;

            // Simple member access expressions for enums (no semantic model needed)
            case MemberAccessExpressionSyntax memberAccess when memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression):
            {
                if (memberAccess.Expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().LastOrDefault() is {} classIdentifier)
                {
                    var valueStr = memberAccess.Name.Identifier.Text;
                    var className = classIdentifier.Identifier.Text;

                    // Parse enums from syntax
                    if ((typeof(T).IsEnum || typeof(T) == typeof(object)) && TryParseEnumFromMemberAccess<T>(className, valueStr, out var enumValue))
                    {
                        return enumValue;
                    }

                    // Name and Tag (no semantic model needed)
                    if (className is nameof(Name) or nameof(Tag) && typeof(T) == typeof(object))
                    {
                        return GetConstantValueFromSemanticModel<T>(semanticModel, node, smartTagKind, valueStr);
                    }
                }
                break;
            }

            // Identifier names (no semantic model needed)
            case IdentifierNameSyntax identifierName:
            {
                // Fast path: parse enums from syntax
                // ReSharper disable once ConvertIfStatementToReturnStatement
                if (typeof(T).IsEnum && TryParseEnumFromMemberAccess<T>(typeof(T).Name, identifierName.Identifier.Text, out var enumValue))
                {
                    return enumValue;
                }

                return GetConstantValueFromSemanticModel<T>(semanticModel, node, smartTagKind, identifierName.Identifier.Text);
            }

            // Tag.* invocations (some semantic model usage)
            case InvocationExpressionSyntax invocation:
            {
                var value = TryProcessTagInvocation<T>(semanticModel, invocation, smartTagKind);
                if (value is not null)
                {
                    return value;
                }
                break;
            }
        }

        return TryGetConstantValueFromSemanticModel<T>(semanticModel, node);
    }

    private T? TryProcessTagInvocation<T>(SemanticModel semanticModel, InvocationExpressionSyntax invocation, SmartTagKind smartTagKind)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return default;
        }

        switch (memberAccess)
        {
            case { Name.Identifier.Text: nameof(Tag.On), Expression: IdentifierNameSyntax { Identifier.Text: nameof(Tag) } }:
                if (invocation.ArgumentList.Arguments is var injectionSitesArgs)
                {
                    var injectionSites = injectionSitesArgs
                        .Select(injectionSiteArg => (Source: injectionSiteArg.Expression, Value: GetConstantValue<string>(semanticModel, injectionSiteArg.Expression, smartTagKind)))
                        .Where(i => !string.IsNullOrWhiteSpace(i.Value))
                        .Select(i => new MdInjectionSite(i.Source, i.Value!))
                        .ToImmutableArray();

                    return (T)MdTag.CreateTagOnValue(invocation, injectionSites);
                }
                // ReSharper disable once HeuristicUnreachableCode
                break;

            case { Name: GenericNameSyntax { TypeArgumentList.Arguments: [{} typeArg] }, Name.Identifier.Text: nameof(Tag.OnConstructorArg), Expression: IdentifierNameSyntax { Identifier.Text: nameof(Tag) } }:
                if (invocation.ArgumentList.Arguments is [{} ctorArgName])
                {
                    var name = GetRequiredConstantValue<string>(semanticModel, ctorArgName.Expression, smartTagKind);
                    var ctor = GetTypeSymbol<ITypeSymbol>(semanticModel, typeArg)
                        .GetMembers()
                        .OfType<IMethodSymbol>()
                        .FirstOrDefault(i =>
                            IsAccessible(i)
                            && !i.IsStatic
                            && i.Parameters.Any(p => wildcardMatcher.Match(name.AsSpan(), p.Name.AsSpan())));

                    if (ctor is null)
                    {
                        throw new CompileErrorException(
                            string.Format(Strings.Error_Template_NoAccessibleConstructor, typeArg, name),
                            ImmutableArray.Create(locationProvider.GetLocation(invocation)),
                            LogId.ErrorNoAccessibleConstructorForTagOn,
                            nameof(Strings.Error_Template_NoAccessibleConstructor));
                    }

                    var injectionSite = injectionSiteFactory.CreateInjectionSite(ctorArgName.Expression, ctor, name);
                    return (T)MdTag.CreateTagOnValue(invocation, ImmutableArray.Create(injectionSite));
                }
                break;

            case { Name: GenericNameSyntax { TypeArgumentList.Arguments: [{} typeArg] }, Name.Identifier.Text: nameof(Tag.OnMethodArg), Expression: IdentifierNameSyntax { Identifier.Text: nameof(Tag) } }:
                if (invocation.ArgumentList.Arguments is [{} methodNameArg, {} methodArgName])
                {
                    var methodName = GetRequiredConstantValue<string>(semanticModel, methodNameArg.Expression, smartTagKind);
                    var methodArg = GetRequiredConstantValue<string>(semanticModel, methodArgName.Expression, smartTagKind);
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
                        throw new CompileErrorException(
                            string.Format(Strings.Error_Template_NoAccessibleMethod, typeArg, methodName, methodArg),
                            ImmutableArray.Create(locationProvider.GetLocation(invocation)),
                            LogId.ErrorNoAccessibleMethodForTagOn,
                            nameof(Strings.Error_Template_NoAccessibleMethod));
                    }

                    var injectionSite = injectionSiteFactory.CreateInjectionSite(methodArgName.Expression, method, methodArg);
                    if (MdTag.CreateTagOnValue(invocation, ImmutableArray.Create(injectionSite)) is T tagValue)
                    {
                        return tagValue;
                    }
                }
                break;

            case { Name: GenericNameSyntax { TypeArgumentList.Arguments: [{} typeArg] }, Name.Identifier.Text: nameof(Tag.OnMember), Expression: IdentifierNameSyntax { Identifier.Text: nameof(Tag) } }:
                if (invocation.ArgumentList.Arguments is [{} memberNameArg])
                {
                    var name = GetRequiredConstantValue<string>(semanticModel, memberNameArg.Expression, smartTagKind);
                    var type = GetTypeSymbol<ITypeSymbol>(semanticModel, typeArg);
                    var member = type
                        .GetMembers()
                        .FirstOrDefault(i =>
                            IsAccessible(i)
                            && i is IFieldSymbol { IsReadOnly: false, IsConst: false } or IPropertySymbol { IsReadOnly: false, SetMethod: not null }
                            && wildcardMatcher.Match(name.AsSpan(), i.Name.AsSpan()));

                    if (member is null)
                    {
                        throw new CompileErrorException(
                            string.Format(Strings.Error_Template_NoAccessibleFieldOrProperty, name, typeArg),
                            ImmutableArray.Create(locationProvider.GetLocation(invocation)),
                            LogId.ErrorNoAccessibleFieldOrPropertyForTagOn,
                            nameof(Strings.Error_Template_NoAccessibleFieldOrProperty));
                    }

                    var injectionSite = injectionSiteFactory.CreateInjectionSite(memberNameArg, type, name);
                    return (T)MdTag.CreateTagOnValue(invocation, ImmutableArray.Create(injectionSite));
                }
                break;
        }

        return default;
    }

    private static bool TryParseEnumFromMemberAccess<T>(string enumTypeName, string valueStr, [NotNullWhen(true)] out T? val)
    {
        val = default;
        switch (enumTypeName)
        {
            case nameof(CompositionKind) when Enum.TryParse<CompositionKind>(valueStr, out var compositionKindValue) && compositionKindValue is T compositionKind:
                val = compositionKind;
                return true;

            case nameof(Lifetime) when Enum.TryParse<Lifetime>(valueStr, out var lifetimeValue) && lifetimeValue is T lifetime:
                val = lifetime;
                return true;

            case nameof(RootKinds) when Enum.TryParse<RootKinds>(valueStr, out var rootKindsValue) && rootKindsValue is T rootKinds:
                val = rootKinds;
                return true;

            case nameof(SetupContextKind) when Enum.TryParse<SetupContextKind>(valueStr, out var setupContextKindValue) && setupContextKindValue is T setupContextKind:
                val = setupContextKind;
                return true;

            case nameof(Hint) when Enum.TryParse<Hint>(valueStr, out var hintValue) && hintValue is T hint:
                val = hint;
                return true;

            default:
                return false;
        }
    }

    private static bool TryGetValueOf<T>(object? obj, [NotNullWhen(true)] out T? val)
    {
        // ReSharper disable once ConvertIfStatementToSwitchStatement
        if (obj is null)
        {
            val = default;
            return false;
        }

        if (obj is T valOfT)
        {
            val = valOfT;
            return true;
        }

        if (typeof(T).IsEnum)
        {
            val = (T)Enum.ToObject(typeof(T), obj);
            return true;
        }

        val = default;
        return false;
    }

    private static T? TryGetConstantValueFromSemanticModel<T>(SemanticModel semanticModel, SyntaxNode node)
    {
        // ReSharper disable once InvertIf
        if (semanticModel.SyntaxTree == node.SyntaxTree)
        {
            // Try GetConstantValue first (lighter weight operation)
            var optionalValue = semanticModel.GetConstantValue(node);
            if (TryGetValueOf<T>(optionalValue.Value, out var val))
            {
                return val;
            }

            // Try GetOperation as last resort (expensive operation!)
            var operation = semanticModel.GetOperation(node);
            if (TryGetValueOf<T>(operation?.ConstantValue.Value, out var val2))
            {
                return val2;
            }

            if (typeof(T) == typeof(object) && operation is ITypeOfOperation { TypeOperand: T val3 })
            {
                return val3;
            }
        }

        return default;
    }

    private T? GetConstantValueFromSemanticModel<T>(SemanticModel semanticModel, SyntaxNode node, SmartTagKind smartTagKind, string text)
    {
        // ReSharper disable once InvertIf
        if (semanticModel.SyntaxTree == node.SyntaxTree)
        {
            var value = TryGetConstantValueFromSemanticModel<T?>(semanticModel, node);
            if (value != null)
            {
                return value;
            }
        }

        return text switch
        {
            nameof(Tag.Type) => (T)(object)Tag.Type,
            nameof(Tag.Unique) => (T)(object)Tag.Unique,
            nameof(Tag.Any) => (T)(object)Tag.Any,
            _ => (T)smartTags.Register(smartTagKind, text)
        };
    }

    public bool IsValidNamespace(INamespaceSymbol? namespaceSymbol) =>
        namespaceSymbol is { IsImplicitlyDeclared: false };

    private ITypeSymbol? GetTypeSymbolCore(SemanticModel semanticModel, SyntaxNode node)
    {
        if (node.SyntaxTree != semanticModel.SyntaxTree)
        {
            return ResolveTypeSymbol(semanticModel, node);
        }

        var cache = _typeSymbolCaches.GetValue(semanticModel, static _ => new TypeSymbolCache());
        lock (cache.SyncRoot)
        {
            if (cache.TypeSymbols.TryGetValue(node, out var entry))
            {
                return entry.Symbol;
            }
        }

        var symbol = ResolveTypeSymbol(semanticModel, node);
        var newEntry = new TypeSymbolCacheEntry(symbol);
        lock (cache.SyncRoot)
        {
            if (cache.TypeSymbols.TryGetValue(node, out var entry))
            {
                return entry.Symbol;
            }

            cache.TypeSymbols.Add(node, newEntry);
        }

        return symbol;
    }

    private ITypeSymbol? ResolveTypeSymbol(SemanticModel semanticModel, SyntaxNode node)
    {
        var typeInfo = semanticModel.GetTypeInfo(node, cancellationToken);
        var typeSymbol = typeInfo.Type ?? typeInfo.ConvertedType;
        return typeSymbol?.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
    }

    private sealed class TypeSymbolCache
    {
        public object SyncRoot { get; } = new();

        public Dictionary<SyntaxNode, TypeSymbolCacheEntry> TypeSymbols { get; } =
            new(SyntaxNodeReferenceComparer.Instance);
    }

    private readonly struct TypeSymbolCacheEntry(ITypeSymbol? symbol)
    {
        public ITypeSymbol? Symbol { get; } = symbol;
    }

    private sealed class SyntaxNodeReferenceComparer : IEqualityComparer<SyntaxNode>
    {
        public static SyntaxNodeReferenceComparer Instance { get; } = new();

        public bool Equals(SyntaxNode? x, SyntaxNode? y) => ReferenceEquals(x, y);

        public int GetHashCode(SyntaxNode obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
