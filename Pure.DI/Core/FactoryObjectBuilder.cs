// ReSharper disable All
namespace Pure.DI.Core;

using Core;

// ReSharper disable once ClassNeverInstantiated.Global
internal class FactoryObjectBuilder : IObjectBuilder
{
    private readonly IBuildContext _buildContext;
    private readonly IMemberNameService _memberNameService;
    private readonly IStringTools _stringTools;
    private readonly Func<FactoryRewriter> _factoryRewriter;

    public FactoryObjectBuilder(
        IBuildContext buildContext,
        IMemberNameService memberNameService,
        IStringTools stringTools,
        Func<FactoryRewriter> factoryRewriter)
    {
        _buildContext = buildContext;
        _memberNameService = memberNameService;
        _stringTools = stringTools;
        _factoryRewriter = factoryRewriter;
    }

    public ExpressionSyntax? TryBuild(IBuildStrategy buildStrategy, Dependency dependency)
    {
        var factory = dependency.Binding.Factory;
        if (factory != default)
        {
            if (factory.ExpressionBody != default)
            {
                return Rewrite(buildStrategy, dependency, factory, factory.ExpressionBody);
            }

            if (factory.Block != default)
            {
                var type = dependency.Implementation.TypeSyntax;
                var memberKey = new MemberKey($"Create{_stringTools.ConvertToTitle(type.ToString() ?? String.Empty)}", dependency);
                var factoryMethod = (MethodDeclarationSyntax)_buildContext.GetOrAddMember(memberKey, () =>
                {
                    var factoryName = _buildContext.NameService.FindName(memberKey);
                    var block = Rewrite(buildStrategy, dependency, factory, factory.Block)!;
                    return SyntaxFactory.MethodDeclaration(type, SyntaxFactory.Identifier(factoryName))
                        .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxRepo.AggressiveInliningAttr))
                        .AddParameterListParameters(factory.Parameter.WithType(SyntaxFactory.ParseTypeName(_memberNameService.GetName(MemberNameKind.ContextClass))))
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                        .AddBodyStatements(block);
                });

                return SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(factoryMethod.Identifier))
                    .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(_memberNameService.GetName(MemberNameKind.ContextField))));
            }
        }

        return SyntaxFactory.DefaultExpression(SyntaxFactory.ParseTypeName(dependency.Implementation.Type.Name));
    }

    private T? Rewrite<T>(IBuildStrategy buildStrategy, Dependency dependency, SimpleLambdaExpressionSyntax factory, T resultExpression)
        where T : SyntaxNode =>
        ((T?)_factoryRewriter()
            .Initialize(dependency, buildStrategy, factory.Parameter.Identifier)
            .Visit(resultExpression))
        ?.WithCommentBefore($"// {dependency.Binding}");
}