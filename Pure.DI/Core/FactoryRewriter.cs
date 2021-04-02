// ReSharper disable IdentifierTypo
namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class FactoryRewriter: CSharpSyntaxRewriter
    {
        private readonly TypeResolveDescription _typeDescription;
        private readonly SyntaxToken _contextIdentifier;
        private readonly ICollection<BindingMetadata> _additionalBindings;

        public FactoryRewriter(
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
                    if (!namedType.Equals(constructedType, SymbolEqualityComparer.Default))
                    {
                        _additionalBindings.Add(new BindingMetadata(_typeDescription.Binding, constructedType));
                    }
                }
            }
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
    }
}