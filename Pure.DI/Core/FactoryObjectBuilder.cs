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

        public ExpressionSyntax TryBuild(
            ITypeResolver typeResolver,
            IBindingExpressionStrategy bindingExpressionStrategy,
            TypeResolveDescription typeDescription)
        {
            var factory = typeDescription.Binding.Factory;
            ExpressionSyntax? resultExpression = factory;
            if (factory?.ExpressionBody != null)
            {
                resultExpression = factory.ExpressionBody;
            }
            else
            {
                if (factory?.Block != null)
                {
                    var key = new MemberKey("Lambda", typeDescription.Type, null);
                    var methotName = _buildContext.NameService.FindName(key);
                    var method = _buildContext.GetOrAddMember(key, () =>
                    {
                        var type = typeDescription.Type.ToTypeSyntax(typeDescription.SemanticModel);
                        return SyntaxFactory.MethodDeclaration(
                                type,
                                SyntaxFactory.Identifier(methotName))
                            .AddParameterListParameters(factory.Parameter.WithType(SyntaxRepo.ContextTypeSyntax))
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                            .AddBodyStatements(factory.Block.Statements.ToArray());
                    });

                    resultExpression = 
                        SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(methotName))
                            .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(SyntaxRepo.SharedContextName)));
                }
            }

            if (factory != null && resultExpression != null)
            {
                return (ExpressionSyntax) new FactoryRewriter(
                        typeDescription,
                        bindingExpressionStrategy,
                        factory.Parameter.Identifier,
                        _buildContext)
                    .Visit(resultExpression);
            }

            return SyntaxFactory.DefaultExpression(SyntaxFactory.ParseTypeName(typeDescription.Type.Name));
        }
    }
}
