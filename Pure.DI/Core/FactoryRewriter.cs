// ReSharper disable IdentifierTypo
namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Operations;

    internal class FactoryRewriter: CSharpSyntaxRewriter
    {
        private readonly TypeDescription _typeDescription;
        private readonly IBuildStrategy _buildStrategy;
        private readonly SyntaxToken _contextIdentifier;
        private readonly IBuildContext _buildContext;

        public FactoryRewriter(
            TypeDescription typeDescription,
            IBuildStrategy buildStrategy,
            SyntaxToken contextIdentifier,
            IBuildContext buildContext)
            : base(true)
        {
            _typeDescription = typeDescription;
            _buildStrategy = buildStrategy;
            _contextIdentifier = contextIdentifier;
            _buildContext = buildContext;
        }

        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var operation = _typeDescription.SemanticModel.GetOperation(node);
            if (operation is IInvocationOperation invocationOperation)
            {
                if (
                    invocationOperation.TargetMethod.IsGenericMethod
                    && invocationOperation.TargetMethod.TypeArguments.Length == 1)
                {
                    if (invocationOperation.TargetMethod.Name == nameof(IContext.Resolve)
                        && typeof(IContext).Equals(invocationOperation.TargetMethod.ContainingType, _typeDescription.SemanticModel)
                        && SymbolEqualityComparer.Default.Equals(invocationOperation.TargetMethod.TypeArguments[0], invocationOperation.TargetMethod.ReturnType))
                    {
                        var tag = invocationOperation.Arguments.Length == 1 ? invocationOperation.Arguments[0].Value.Syntax as ExpressionSyntax : null;
                        var typeDescription = _buildContext.TypeResolver.Resolve(_typeDescription.TypesMap.ConstructType(invocationOperation.TargetMethod.ReturnType), tag);
                        return _buildStrategy.Build(typeDescription);
                    }
                }
            }

            return base.VisitInvocationExpression(node);
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

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (
                node.Kind() == SyntaxKind.SimpleMemberAccessExpression
                && node.Expression is IdentifierNameSyntax identifierName
                && identifierName.ToString() == _contextIdentifier.Text)
            {
                return Visit(node.ChildNodes().OfType<GenericNameSyntax>().FirstOrDefault());
            }

            return base.VisitMemberAccessExpression(node);
        }

        private void ReplaceTypes(IList<TypeSyntax> args)
        {
            for (var i = 0; i < args.Count; i++)
            {
                var typeSymbol = _typeDescription.SemanticModel.GetTypeInfo(args[i]).Type;
                if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
                {
                    var constructedType = _typeDescription.TypesMap.ConstructType(namedTypeSymbol);
                    args[i] = constructedType.ToTypeSyntax(_typeDescription.SemanticModel);
                    if (!typeSymbol.Equals(constructedType, SymbolEqualityComparer.Default))
                    {
                        _buildContext.AddBinding(new BindingMetadata(_typeDescription.Binding, constructedType));
                    }
                }
            }
        }
    }
}