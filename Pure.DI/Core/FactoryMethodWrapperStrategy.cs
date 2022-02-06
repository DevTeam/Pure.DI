// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using NS35EBD81B;

internal class FactoryMethodWrapperStrategy : IWrapperStrategy
{
    private readonly ICannotResolveExceptionFactory _cannotResolveExceptionFactory;
    private readonly IDiagnostic _diagnostic;
    private readonly IBuildContext _buildContext;
    private readonly Func<IBuildStrategy> _buildStrategy;
    private readonly IIncludeTypeFilter _includeTypeFilter;
    private readonly IStringTools _stringTools;

    public FactoryMethodWrapperStrategy(
        ICannotResolveExceptionFactory cannotResolveExceptionFactory,
        IDiagnostic diagnostic,
        IBuildContext buildContext,
        Func<IBuildStrategy> buildStrategy,
        IIncludeTypeFilter includeTypeFilter,
        IStringTools stringTools)
    {
        _cannotResolveExceptionFactory = cannotResolveExceptionFactory;
        _diagnostic = diagnostic;
        _buildContext = buildContext;
        _buildStrategy = buildStrategy;
        _includeTypeFilter = includeTypeFilter;
        _stringTools = stringTools;
    }

    public ExpressionSyntax Build(SemanticType resolvingType, Dependency dependency, ExpressionSyntax objectBuildExpression)
    {
        var factoryType = _buildContext.Compilation.GetTypeByMetadataName(typeof(IFactory).FullName.ReplaceNamespace());
        if (factoryType == null)
        {
            const string error = "Factory is not supported.";
            _diagnostic.Error(Diagnostics.Error.Unsupported, error);
            throw new HandledException(error);
        }

        var factoryTypeDescriptions = _buildContext.TypeResolver.Resolve(new SemanticType(factoryType, dependency.Implementation));
        return factoryTypeDescriptions.Aggregate(objectBuildExpression, (syntax, factoryTypeDescription) => Wrap(factoryType, resolvingType, dependency, syntax, factoryTypeDescription));
    }

    private ExpressionSyntax Wrap(ISymbol factoryType, SemanticType resolvingType, Dependency dependency, ExpressionSyntax objectBuildExpression, Dependency factoryTypeDescription)
    {
        if (factoryType.Equals(resolvingType.Type, SymbolEqualityComparer.Default))
        {
            return objectBuildExpression;
        }

        if (dependency.Implementation.ImplementsInterface<IFactory>())
        {
            return objectBuildExpression;
        }

        if (!_includeTypeFilter.IsAccepted(factoryTypeDescription.Implementation, dependency.Implementation))
        {
            return objectBuildExpression;
        }

        var methodKey = new MemberKey($"FactoryMethodResolve_{_stringTools.ConvertToTitle(dependency.Implementation.ToString())}", dependency);
        var createMethodSyntax = _buildContext.GetOrAddMember(methodKey, () =>
            SyntaxFactory.MethodDeclaration(dependency.Implementation, _buildContext.NameService.FindName(methodKey))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxRepo.AggressiveInliningAttr))
                .AddBodyStatements(SyntaxFactory.ReturnStatement(objectBuildExpression)));

        var factoryExpression = _buildStrategy().TryBuild(factoryTypeDescription, factoryTypeDescription.Implementation);
        if (factoryExpression == null)
        {
            throw _cannotResolveExceptionFactory.Create(dependency.Binding, dependency.Tag, "a factory");
        }

        return SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, factoryExpression, SyntaxFactory.GenericName(nameof(IFactory.Create)).AddTypeArgumentListArguments(resolvingType)))
            .AddArgumentListArguments(
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(createMethodSyntax.Identifier)),
                SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(dependency.Implementation)),
                SyntaxFactory.Argument(dependency.Tag ?? SyntaxFactory.DefaultExpression(SyntaxRepo.ObjectTypeSyntax)));
    }
}