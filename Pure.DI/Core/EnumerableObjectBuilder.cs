// ReSharper disable MergeIntoNegatedPattern
namespace Pure.DI.Core;

// ReSharper disable once ClassNeverInstantiated.Global
internal class EnumerableObjectBuilder : IObjectBuilder
{
    private readonly IBuildContext _buildContext;
    private readonly ITypeResolver _typeResolver;
    private readonly IStringTools _stringTools;

    public EnumerableObjectBuilder(
        IBuildContext buildContext,
        ITypeResolver typeResolver,
        IStringTools stringTools)
    {
        _buildContext = buildContext;
        _typeResolver = typeResolver;
        _stringTools = stringTools;
    }

    public Optional<ExpressionSyntax> TryBuild(IBuildStrategy buildStrategy, Dependency dependency)
    {
        if (
            dependency.Implementation.Type is not INamedTypeSymbol namedTypeSymbol
            || namedTypeSymbol.TypeArguments.Length != 1)
        {
            throw new InvalidOperationException($"Invalid enumerable type {dependency.Implementation}.");
        }

        var elementType = namedTypeSymbol.TypeArguments[0];
        var memberKey = new MemberKey($"EnumerableOf{_stringTools.ConvertToTitle(elementType.Name)}", dependency);

        var factoryMethod = _buildContext.GetOrAddMember(memberKey, () =>
        {
            var resolvingType = new SemanticType(elementType, dependency.Implementation);
            var yields =
                from element in _typeResolver.Resolve(resolvingType)
                let objectCreationExpression = buildStrategy.TryBuild(element, resolvingType)
                where objectCreationExpression.HasValue
                select (StatementSyntax)SyntaxRepo.YieldStatement(SyntaxKind.YieldReturnStatement).WithExpression(objectCreationExpression.Value.WithSpace());

            var factoryName = _buildContext.NameService.FindName(memberKey);
            var type = dependency.Implementation.TypeSyntax;
            return SyntaxRepo.MethodDeclaration(type, SyntaxFactory.Identifier(factoryName))
                .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxRepo.AggressiveInliningAttr))
                .AddParameterListParameters()
                .AddModifiers(SyntaxKind.PrivateKeyword.WithSpace(), SyntaxKind.StaticKeyword.WithSpace())
                .AddBodyStatements(yields.DefaultIfEmpty(SyntaxRepo.YieldStatement(SyntaxKind.YieldBreakStatement)).ToArray());
        });

        return SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(factoryMethod.Identifier.Text));
    }
}