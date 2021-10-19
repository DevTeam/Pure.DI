// ReSharper disable All
namespace Pure.DI.Core
{
    using System;
    using System.Linq;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class FactoryObjectBuilder: IObjectBuilder
    {
        private readonly IBuildContext _buildContext;
        private readonly IMemberNameService _memberNameService;
        private readonly ICannotResolveExceptionFactory _cannotResolveExceptionFactory;
        private readonly IStringTools _stringTools;
        private readonly ICache<InvocationExpressionSyntax, bool> _requiresCall;

        public FactoryObjectBuilder(
            IBuildContext buildContext,
            IMemberNameService memberNameService,
            ICannotResolveExceptionFactory cannotResolveExceptionFactory,
            IStringTools stringTools,
            [Tag(Tags.ContainerScope)] ICache<InvocationExpressionSyntax, bool> requiresCall)
        {
            _buildContext = buildContext;
            _memberNameService = memberNameService;
            _cannotResolveExceptionFactory = cannotResolveExceptionFactory;
            _stringTools = stringTools;
            _requiresCall = requiresCall;
        }

        public ExpressionSyntax? TryBuild(IBuildStrategy buildStrategy, Dependency dependency)
        {
            var factory = dependency.Binding.Factory;
            ExpressionSyntax? resultExpression = factory;
            if (factory?.ExpressionBody != null)
            {
                resultExpression = factory.ExpressionBody;
            }
            else
            {
                if (factory?.Block != null)
                {
                    var memberKey = new MemberKey($"Create{_stringTools.ConvertToTitle(dependency.Binding.Implementation?.ToString() ?? String.Empty)}", dependency);
                    var factoryMethod = (MethodDeclarationSyntax)_buildContext.GetOrAddMember(memberKey, () =>
                    {
                        var factoryName = _buildContext.NameService.FindName(memberKey);
                        var type = dependency.Implementation.TypeSyntax;
                        return SyntaxFactory.MethodDeclaration(type, SyntaxFactory.Identifier(factoryName))
                            .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxRepo.AggressiveInliningAttr))
                            .AddParameterListParameters(factory.Parameter.WithType(SyntaxFactory.ParseTypeName(_memberNameService.GetName(MemberNameKind.ContextClass))))
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                            .AddBodyStatements(factory.Block.Statements.ToArray());
                    });

                    resultExpression = 
                        SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(factoryMethod.Identifier))
                            .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(_memberNameService.GetName(MemberNameKind.ContextField))));
                }
            }

            if (factory != null && resultExpression != null)
            {
                return ((ExpressionSyntax?) new FactoryRewriter(
                        dependency,
                        buildStrategy,
                        factory.Parameter.Identifier,
                        _buildContext,
                        _cannotResolveExceptionFactory,
                        _requiresCall)
                    .Visit(resultExpression))
                    ?.WithCommentBefore($"// {dependency.Binding}");
            }

            return SyntaxFactory.DefaultExpression(SyntaxFactory.ParseTypeName(dependency.Implementation.Type.Name));
        }
    }
}
