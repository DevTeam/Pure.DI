namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class FactoryObjectBuilder: IObjectBuilder
    {
        public ExpressionSyntax TryBuild(
            TypeResolveDescription typeDescription,
            ICollection<BindingMetadata> additionalBindings,
            int level = 0)
        {
            var factory = typeDescription.Binding.Factory;
            ExpressionSyntax resultExpression = factory;
            if (factory.ExpressionBody != null)
            {
                resultExpression = factory.ExpressionBody;
            }
            else
            {
                if (factory.Block != null)
                {
                    var funcName = SyntaxFactory.GenericName(nameof(Func<object>))
                        .WithTypeArgumentList(
                            SyntaxFactory.TypeArgumentList()
                                .AddArguments(ResolverBuilder.ContextTypeSyntax)
                                .AddArguments(typeDescription.Type.ToTypeSyntax(typeDescription.SemanticModel)));

                    var createFunc = SyntaxFactory.ObjectCreationExpression(funcName)
                        .AddArgumentListArguments(SyntaxFactory.Argument(typeDescription.Binding.Factory));

                    resultExpression = SyntaxFactory.InvocationExpression(createFunc)
                        .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(ResolverBuilder.SharedContextName)));
                }
            }

            return (ExpressionSyntax)new FactoryRewriter(typeDescription, factory.Parameter.Identifier, additionalBindings)
                .Visit(resultExpression);
        }
    }
}
