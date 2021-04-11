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

        public ExpressionSyntax TryBuild(IBindingExpressionStrategy bindingExpressionStrategy, TypeDescription typeDescription)
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
                    var memberKey = new MemberKey($"Create{typeDescription.Type.Name}", typeDescription.Type, null);
                    var factoryName = _buildContext.NameService.FindName(memberKey);
                    var factoryMethod = _buildContext.GetOrAddMember(memberKey, () =>
                    {
                        var type = typeDescription.Type.ToTypeSyntax(typeDescription.SemanticModel);
                        return SyntaxFactory.MethodDeclaration(type, SyntaxFactory.Identifier(factoryName))
                            .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxRepo.AggressiveOptimizationAndInliningAttr))
                            .AddParameterListParameters(factory.Parameter.WithType(SyntaxRepo.ContextTypeSyntax))
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                            .AddBodyStatements(factory.Block.Statements.ToArray());
                    });

                    resultExpression = 
                        SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(factoryName))
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
