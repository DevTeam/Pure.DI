namespace Pure.DI.Core;

using NS35EBD81B;

// ReSharper disable once ClassNeverInstantiated.Global
internal class FactoryWrapperStrategy : IWrapperStrategy
{
    private readonly IBuildContext _buildContext;
    private readonly ICannotResolveExceptionFactory _cannotResolveExceptionFactory;
    private readonly Func<IBuildStrategy> _buildStrategy;
    private readonly IIncludeTypeFilter _includeTypeFilter;
    private readonly IStringTools _stringTools;

    public FactoryWrapperStrategy(
        IBuildContext buildContext,
        ICannotResolveExceptionFactory cannotResolveExceptionFactory,
        Func<IBuildStrategy> buildStrategy,
        IIncludeTypeFilter includeTypeFilter,
        IStringTools stringTools)
    {
        _buildContext = buildContext;
        _cannotResolveExceptionFactory = cannotResolveExceptionFactory;
        _buildStrategy = buildStrategy;
        _includeTypeFilter = includeTypeFilter;
        _stringTools = stringTools;
    }

    public ExpressionSyntax Build(SemanticType resolvingType, Dependency dependency, ExpressionSyntax objectBuildExpression)
    {
        var baseFactoryType = dependency.Implementation.SemanticModel.Compilation.GetTypeByMetadataName(typeof(IFactory<>).FullName.ReplaceNamespace());
        var factoryType = baseFactoryType?.Construct(resolvingType.Type);
        if (factoryType == null)
        {
            throw _cannotResolveExceptionFactory.Create(dependency.Binding, dependency.Tag, new CodeError[] { new($"cannot construct a factory of the type \"{resolvingType.Type}\"")});
        }

        if (dependency.Implementation.Type is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol)
        {
            var baseGenericType = baseFactoryType?.ConstructUnboundGenericType();
            if (namedTypeSymbol.Interfaces.Any(i => i.IsGenericType && i.ConstructUnboundGenericType().Equals(baseGenericType, SymbolEqualityComparer.Default)))
            {
                return objectBuildExpression;
            }
        }

        var factoryTypes = _buildContext.TypeResolver.Resolve(new SemanticType(factoryType, dependency.Implementation))
            .Where(i => Equals(i.Tag?.ToString(), dependency.Tag?.ToString()));

        return factoryTypes.Aggregate(objectBuildExpression, (syntax, factoryDependency) => Wrap(syntax, factoryDependency, dependency));
    }

    private ExpressionSyntax Wrap(ExpressionSyntax objectBuildExpression, Dependency factoryDependency, Dependency dependency)
    {
        if (!_includeTypeFilter.IsAccepted(factoryDependency.Implementation, dependency.Implementation))
        {
            return objectBuildExpression;
        }

        var instance = _buildStrategy().TryBuild(factoryDependency, factoryDependency.Implementation);
        if (!instance.HasValue)
        {
            throw _cannotResolveExceptionFactory.Create(factoryDependency.Binding, factoryDependency.Tag, instance.Errors);
        }

        var methodKey = new MemberKey($"FactoryWrapperResolve_{_stringTools.ConvertToTitle(dependency.Implementation.ToString())}", dependency);
        var createMethodSyntax = _buildContext.GetOrAddMember(methodKey, () =>
            SyntaxRepo.MethodDeclaration(dependency.Implementation, _buildContext.NameService.FindName(methodKey))
                .AddModifiers(SyntaxKind.PrivateKeyword.WithSpace(), SyntaxKind.StaticKeyword.WithSpace())
                .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxRepo.AggressiveInliningAttr))
                .AddBodyStatements(SyntaxRepo.ReturnStatement(objectBuildExpression)));

        return SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, instance.Value, SyntaxFactory.IdentifierName(nameof(IFactory<object>.Create))))
            .AddArgumentListArguments(
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(createMethodSyntax.Identifier)),
                SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(dependency.Implementation)),
                SyntaxFactory.Argument(dependency.Tag ?? SyntaxFactory.DefaultExpression(SyntaxRepo.ObjectTypeSyntax)));
    }
}