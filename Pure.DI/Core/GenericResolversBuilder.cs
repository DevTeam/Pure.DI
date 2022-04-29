// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Pure.DI.Core;

internal class GenericResolversBuilder : IMembersBuilder
{
    private readonly ResolverMetadata _metadata;
    private readonly IBuildContext _buildContext;
    private readonly ITypeResolver _typeResolver;
    private readonly IBuildStrategy _buildStrategy;
    private readonly IStatementsFinalizer _statementsFinalizer;
    private readonly IStaticResolverNameProvider _staticResolverNameProvider;

    public GenericResolversBuilder(
        ResolverMetadata metadata,
        IBuildContext buildContext,
        ITypeResolver typeResolver,
        IBuildStrategy buildStrategy,
        IStatementsFinalizer statementsFinalizer,
        IStaticResolverNameProvider staticResolverNameProvider)
    {
        _metadata = metadata;
        _buildContext = buildContext;
        _typeResolver = typeResolver;
        _buildStrategy = buildStrategy;
        _statementsFinalizer = statementsFinalizer;
        _staticResolverNameProvider = staticResolverNameProvider;
    }

    public int Order => 2;

    public IEnumerable<MemberDeclarationSyntax> BuildMembers(SemanticModel semanticModel) =>
        BuildMethods()
            .Select(method => method.WithBody(_statementsFinalizer.AddFinalizationStatements(method.Body)));

    private IEnumerable<MethodDeclarationSyntax> BuildMethods()
    {
        var methods =
            from binding in _metadata.Bindings.Concat(_buildContext.AdditionalBindings).ToArray()
            where !binding.FromProbe
            from dependency in binding.Dependencies
            where !dependency.IsComposedGenericTypeMarker
            let minAccessibility = GetAccessibility(dependency.Type).Min()
            where minAccessibility >= Accessibility.Internal
            let accessibility = minAccessibility == Accessibility.Public ? SyntaxKind.PublicKeyword : SyntaxKind.InternalKeyword
            let methodName = _staticResolverNameProvider.GetName(dependency)
            let method = SyntaxFactory.MethodDeclaration(dependency.TypeSyntax, methodName)
                .AddModifiers(SyntaxFactory.Token(accessibility), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxRepo.AggressiveInliningAttr))
            let tags = binding.GetTags(dependency).ToArray()
            from tag in tags.DefaultIfEmpty(default)
            let resolvedDependency = _typeResolver.Resolve(dependency, tag)
            where resolvedDependency.IsResolved
            select (method, tag, dependency, resolvedDependency);

        var methodGroups = methods.GroupBy(i => i.method, MethodComparer.Shared);
        foreach (var methodGroup in methodGroups)
        {
            var items = methodGroup.ToArray();
            var method = methodGroup.Key;
            var uniqueItems = items.GroupBy(i => i.tag?.ToString()).Select(i => i.First()).ToArray();
            var itemsByBinding = uniqueItems.GroupBy(i => i.resolvedDependency.Binding, BindingComparer.Shared);
            foreach (var item in itemsByBinding)
            {
                var tagItems = item.ToArray();
                var first = tagItems.First();
                var checkTags = tagItems.Skip(1).Aggregate(CreateCheckTagExpression(tagItems.First().tag), (current, next) => SyntaxFactory.BinaryExpression(SyntaxKind.LogicalOrExpression, current, CreateCheckTagExpression(next.tag)));
                var objectExpression = _buildStrategy.TryBuild(first.resolvedDependency, first.dependency);
                if (objectExpression.HasValue)
                {
                    method = method.AddBodyStatements(SyntaxFactory.IfStatement(checkTags, SyntaxFactory.ReturnStatement(objectExpression.Value)));
                }
            }

            foreach (var defaultItem in items.Where(i => i.tag == default).Reverse().Take(1))
            {
                var objectExpression = _buildStrategy.TryBuild(defaultItem.resolvedDependency, defaultItem.dependency);
                if (objectExpression.HasValue)
                {
                    yield return methodGroup.Key.AddBodyStatements(SyntaxFactory.ReturnStatement(objectExpression.Value));
                }
            }

            if (method.Body?.Statements.Any() != true)
            {
                continue;
            }

            var tagType = SyntaxRepo.ObjectTypeSyntax;
            if ((_buildContext.Compilation.Options.NullableContextOptions & NullableContextOptions.Enable) == NullableContextOptions.Enable)
            {
                tagType = SyntaxFactory.NullableType(SyntaxRepo.ObjectTypeSyntax);
            }

            yield return method
                .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("tag")).WithType(tagType))
                .AddBodyStatements(
                    SyntaxFactory.ThrowStatement(
                        SyntaxFactory.ObjectCreationExpression(
                                SyntaxFactory.ParseTypeName("System.ArgumentOutOfRangeException"))
                            .AddArgumentListArguments(SyntaxFactory.Argument("tag".ToLiteralExpression()!))));
        }
    }

    private static ExpressionSyntax CreateCheckTagExpression(ExpressionSyntax? tag)
    {
        return SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("object.Equals"))
            .AddArgumentListArguments(
                SyntaxFactory.Argument(tag ?? SyntaxFactory.DefaultExpression(SyntaxRepo.ObjectTypeSyntax)),
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("tag")));
    }

    private static IEnumerable<Accessibility> GetAccessibility(ISymbol symbol)
    {
        yield return symbol.DeclaredAccessibility == Accessibility.NotApplicable ? Accessibility.Internal : symbol.DeclaredAccessibility;
        switch (symbol)
        {
            case INamedTypeSymbol { IsGenericType: true } namedTypeSymbol:
            {
                var accessibilitySet =
                    from typeArg in namedTypeSymbol.TypeArguments
                    from accessibility in GetAccessibility(typeArg)
                    select accessibility;

                foreach (var accessibility in accessibilitySet)
                {
                    yield return accessibility;
                }

                break;
            }

            case IArrayTypeSymbol arrayTypeSymbol:
                yield return arrayTypeSymbol.ElementType.DeclaredAccessibility;
                break;
        }
    }

    private class MethodComparer : IEqualityComparer<MethodDeclarationSyntax>
    {
        public static readonly IEqualityComparer<MethodDeclarationSyntax> Shared = new MethodComparer();

        private MethodComparer() { }

        public bool Equals(MethodDeclarationSyntax x, MethodDeclarationSyntax y) => x.Identifier.Text == y.Identifier.Text;

        public int GetHashCode(MethodDeclarationSyntax obj) => obj.Identifier.Text.GetHashCode();
    }

    private class BindingComparer : IEqualityComparer<IBindingMetadata>
    {
        public static readonly IEqualityComparer<IBindingMetadata> Shared = new BindingComparer();

        private BindingComparer() { }

        public bool Equals(IBindingMetadata x, IBindingMetadata y) => x.Id == y.Id;

        public int GetHashCode(IBindingMetadata obj) => obj.Id.GetHashCode();
    }
}