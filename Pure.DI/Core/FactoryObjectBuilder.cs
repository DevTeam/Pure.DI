namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
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

            return (ExpressionSyntax)new RemoveContextRewriter(typeDescription, factory.Parameter.Identifier, additionalBindings)
                .Visit(resultExpression);
        }

        private class RemoveContextRewriter: CSharpSyntaxRewriter
        {
            private readonly TypeResolveDescription _typeDescription;
            private readonly SyntaxToken _contextIdentifier;
            private readonly ICollection<BindingMetadata> _additionalBindings;

            public RemoveContextRewriter(
                TypeResolveDescription typeDescription,
                SyntaxToken contextIdentifier,
                ICollection<BindingMetadata> additionalBindings)
            : base(true)
            {
                _typeDescription = typeDescription;
                _contextIdentifier = contextIdentifier;
                _additionalBindings = additionalBindings;
            }

            public override SyntaxNode VisitTypeArgumentList(TypeArgumentListSyntax node)
            {
                var args = node.Arguments.ToArray();
                ReplaceTypes(args);
                return SyntaxFactory.TypeArgumentList().AddArguments(args);
            }

            public override SyntaxNode VisitGenericName(GenericNameSyntax node)
            {
                if (node.IsUnboundGenericName)
                {
                    return node;
                }

                var args = node.TypeArgumentList.Arguments.ToArray();
                ReplaceTypes(args);
                return SyntaxFactory.GenericName(node.Identifier).AddTypeArgumentListArguments(args);
            }

            private void ReplaceTypes(IList<TypeSyntax> args)
            {
                for (var i = 0; i < args.Count; i++)
                {
                    if (_typeDescription.SemanticModel.GetTypeInfo(args[i]).Type is INamedTypeSymbol namedType)
                    {
                        var constructedType = _typeDescription.TypesMap.ConstructType(namedType);
                        args[i] = constructedType.ToTypeSyntax(_typeDescription.SemanticModel);
                        if (!namedType.Equals(constructedType, SymbolEqualityComparer.IncludeNullability))
                        {
                            _additionalBindings.Add(new BindingMetadata(_typeDescription.Binding, constructedType));
                        }
                    }
                }
            }

            public override SyntaxNode VisitPredefinedType(PredefinedTypeSyntax node)
            {
                return base.VisitPredefinedType(node);
            }

            public override SyntaxNode VisitArrayType(ArrayTypeSyntax node)
            {
                return base.VisitArrayType(node);
            }

            public override SyntaxNode VisitPointerType(PointerTypeSyntax node)
            {
                return base.VisitPointerType(node);
            }

            public override SyntaxNode VisitFunctionPointerType(FunctionPointerTypeSyntax node)
            {
                return base.VisitFunctionPointerType(node);
            }

            public override SyntaxNode VisitNullableType(NullableTypeSyntax node)
            {                         
                return base.VisitNullableType(node);
            }

            public override SyntaxNode VisitTupleType(TupleTypeSyntax node)
            {
                return base.VisitTupleType(node);
            }

            public override SyntaxNode VisitOmittedTypeArgument(OmittedTypeArgumentSyntax node)
            {
                return base.VisitOmittedTypeArgument(node);
            }

            public override SyntaxNode VisitRefType(RefTypeSyntax node)
            {
                return base.VisitRefType(node);
            }

            public override SyntaxNode VisitRefTypeExpression(RefTypeExpressionSyntax node)
            {
                return base.VisitRefTypeExpression(node);
            }

            public override SyntaxNode VisitTypeOfExpression(TypeOfExpressionSyntax node)
            {
                return base.VisitTypeOfExpression(node);
            }

            public override SyntaxNode VisitTypePattern(TypePatternSyntax node)
            {
                return base.VisitTypePattern(node);
            }

            public override SyntaxNode VisitTypeParameterList(TypeParameterListSyntax node)
            {
                return base.VisitTypeParameterList(node);
            }

            public override SyntaxNode VisitTypeParameter(TypeParameterSyntax node)
            {
                return base.VisitTypeParameter(node);
            }

            public override SyntaxNode VisitSimpleBaseType(SimpleBaseTypeSyntax node)
            {
                return base.VisitSimpleBaseType(node);
            }

            public override SyntaxNode VisitPrimaryConstructorBaseType(PrimaryConstructorBaseTypeSyntax node)
            {
                return base.VisitPrimaryConstructorBaseType(node);
            }

            public override SyntaxNode VisitTypeParameterConstraintClause(TypeParameterConstraintClauseSyntax node)
            {
                return base.VisitTypeParameterConstraintClause(node);
            }

            public override SyntaxNode VisitTypeConstraint(TypeConstraintSyntax node)
            {
                return base.VisitTypeConstraint(node);
            }

            public override SyntaxNode VisitTypeCref(TypeCrefSyntax node)
            {
                return base.VisitTypeCref(node);
            }

            public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                if (
                    node.Expression is MemberAccessExpressionSyntax memberAccess
                    && memberAccess.Expression is IdentifierNameSyntax identifierName
                    && identifierName.ToString() == _contextIdentifier.Text)
                {
                    var name = (GenericNameSyntax)Visit(memberAccess.DescendantNodes().OfType<GenericNameSyntax>().FirstOrDefault());
                    if (name != null)
                    {
                        return SyntaxFactory.InvocationExpression(name, node.ArgumentList);
                    }
                }

                return base.VisitInvocationExpression(node);
            }
        }
    }
}
