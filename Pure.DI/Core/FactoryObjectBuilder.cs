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

        public FactoryObjectBuilder(IBuildContext buildContext) =>
            _buildContext = buildContext;

        public ExpressionSyntax Build(IBuildStrategy buildStrategy, Dependency dependency)
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
                    var memberKey = new MemberKey($"Create{dependency.Implementation.Type.Name}", dependency.Implementation, null);
                    var factoryMethod = (MethodDeclarationSyntax)_buildContext.GetOrAddMember(memberKey, () =>
                    {
                        var factoryName = _buildContext.NameService.FindName(memberKey);
                        var type = dependency.Implementation.TypeSyntax;
                        return SyntaxFactory.MethodDeclaration(type, SyntaxFactory.Identifier(factoryName))
                            .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxRepo.AggressiveInliningAttr))
                            .AddParameterListParameters(factory.Parameter.WithType(SyntaxRepo.ContextTypeSyntax))
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                            .AddBodyStatements(factory.Block.Statements.ToArray());
                    });

                    resultExpression = 
                        SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(factoryMethod.Identifier))
                            .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(SyntaxRepo.SharedContextName)));
                }
            }

            if (factory != null && resultExpression != null)
            {
                return (ExpressionSyntax) new FactoryRewriter(
                        dependency,
                        buildStrategy,
                        factory.Parameter.Identifier,
                        _buildContext)
                    .Visit(resultExpression);
            }

            return SyntaxFactory.DefaultExpression(SyntaxFactory.ParseTypeName(dependency.Implementation.Type.Name));
        }
    }
}
