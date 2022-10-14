// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Pure.DI.Core;

internal class GenericResolversMembersBuilder : IMembersBuilder
{
    private readonly ResolverMetadata _metadata;
    private readonly IBuildContext _buildContext;
    private readonly ITypeResolver _typeResolver;
    private readonly IBuildStrategy _buildStrategy;
    private readonly IStatementsFinalizer _statementsFinalizer;
    private readonly IStaticResolverNameProvider _staticResolverNameProvider;
    private readonly IStatementsFinalizer[] _statementsFinalizers;
    private readonly IArgumentsSupport _argumentsSupport;
    private readonly IDependencyAccessibility _dependencyAccessibility;

    public GenericResolversMembersBuilder(
        ResolverMetadata metadata,
        IBuildContext buildContext,
        ITypeResolver typeResolver,
        IBuildStrategy buildStrategy,
        IStatementsFinalizer statementsFinalizer,
        IStaticResolverNameProvider staticResolverNameProvider,
        IStatementsFinalizer[] statementsFinalizers,
        IArgumentsSupport argumentsSupport,
        IDependencyAccessibility dependencyAccessibility)
    {
        _metadata = metadata;
        _buildContext = buildContext;
        _typeResolver = typeResolver;
        _buildStrategy = buildStrategy;
        _statementsFinalizer = statementsFinalizer;
        _staticResolverNameProvider = staticResolverNameProvider;
        _statementsFinalizers = statementsFinalizers;
        _argumentsSupport = argumentsSupport;
        _dependencyAccessibility = dependencyAccessibility;
    }

    public int Order => 2;

    public IEnumerable<MemberDeclarationSyntax> BuildMembers(SemanticModel semanticModel) =>
        BuildMethods()
            .Select(method => method.WithBody(_statementsFinalizer.AddFinalizationStatements(method.Body)));
    
    private IEnumerable<MethodDeclarationSyntax> BuildMethods()
    {
        var methods =
            from binding in _metadata.Bindings.Concat(_buildContext.AdditionalBindings).ToArray()
            where binding.BindingType != BindingType.Probe
            from dependency in binding.Dependencies
            where !dependency.IsComposedGenericTypeMarker
            let methodName = _staticResolverNameProvider.GetName(dependency)
            let method = SyntaxRepo.MethodDeclaration(dependency.TypeSyntax, methodName)
                .AddModifiers(_dependencyAccessibility.GetSyntaxKind(dependency).WithSpace(), SyntaxKind.StaticKeyword.WithSpace())
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
                    method = method.AddBodyStatements(SyntaxFactory.IfStatement(checkTags, SyntaxRepo.ReturnStatement(objectExpression.Value)));
                }
            }

            foreach (var defaultItem in items.Where(i => i.tag == default).Reverse().Take(1))
            {
                var objectExpression = _buildStrategy.TryBuild(defaultItem.resolvedDependency, defaultItem.dependency);
                if (objectExpression.HasValue)
                {
                    yield return methodGroup.Key.AddBodyStatements(SyntaxRepo.ReturnStatement(objectExpression.Value));
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

            method = method
                .AddParameterListParameters(SyntaxRepo.Parameter(SyntaxFactory.Identifier("tag")).WithType(tagType))
                .AddBodyStatements(
                    SyntaxRepo.ThrowStatement(
                        SyntaxRepo.ObjectCreationExpression(
                                SyntaxFactory.ParseTypeName("System.ArgumentOutOfRangeException"))
                            .AddArgumentListArguments(SyntaxFactory.Argument("tag".ToLiteralExpression()!))));

            yield return method
                .WithBody(FinalizeBody(method.Body))
                .AddParameterListParameters(_argumentsSupport.GetParameters().ToArray());
        }
    }
    
    private BlockSyntax? FinalizeBody(BlockSyntax? body)
    {
        if (body == default)
        {
            return body;
        }

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var statementsFinalizer in _statementsFinalizers)
        {
            body = statementsFinalizer.AddFinalizationStatements(body);
        }

        return body;
    }

    private static ExpressionSyntax CreateCheckTagExpression(ExpressionSyntax? tag)
    {
        return SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("object.Equals"))
            .AddArgumentListArguments(
                SyntaxFactory.Argument(tag ?? SyntaxFactory.DefaultExpression(SyntaxRepo.ObjectTypeSyntax)),
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("tag")));
    }
}