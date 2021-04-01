namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class BindingExpressionStrategy : IBindingExpressionStrategy
    {
        private readonly SemanticModel _semanticModel;
        private readonly ITypeResolver _typeResolver;
        private readonly ICollection<MemberDeclarationSyntax> _additionalMembers;
        private readonly Dictionary<string, int> _names = new Dictionary<string, int>();

        public BindingExpressionStrategy(SemanticModel semanticModel, ITypeResolver typeResolver, ICollection<MemberDeclarationSyntax> additionalMembers)
        {
            _semanticModel = semanticModel;
            _typeResolver = typeResolver;
            _additionalMembers = additionalMembers;
        }

        [CanBeNull]
        public ExpressionSyntax TryBuild(
            BindingMetadata binding,
            INamedTypeSymbol contractType,
            ExpressionSyntax tag,
            ICollection<BindingMetadata> additionalBindings)
        {
            var typeResolveDescription = _typeResolver.Resolve(contractType, tag);
            var objectExpression = typeResolveDescription.ObjectBuilder.TryBuild(typeResolveDescription, additionalBindings);
            if (objectExpression == null)
            {
                return null;
            }

            switch (binding.Lifetime)
            {
                case Lifetime.Singleton:
                {
                    var resolvedType = typeResolveDescription.Type;
                    var singletonClassName = FindName(string.Join("_", resolvedType.ToMinimalDisplayParts(_semanticModel, 0).Where(i => i.Kind == SymbolDisplayPartKind.ClassName).Select(i => i.ToString())) + "Singleton");

                    var singletonClass = SyntaxFactory.ClassDeclaration(singletonClassName)
                        .AddModifiers(
                            SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                            SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                        .AddMembers(
                            SyntaxFactory.FieldDeclaration(
                                    SyntaxFactory.VariableDeclaration(
                                            resolvedType.ToTypeSyntax(_semanticModel))
                                        .AddVariables(
                                            SyntaxFactory.VariableDeclarator("Shared")
                                                .WithInitializer(SyntaxFactory.EqualsValueClause(objectExpression))
                                        )
                                )
                                .AddModifiers(
                                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                    SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                                    SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))
                        );

                    _additionalMembers.Add(singletonClass);
                    objectExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ParseName(singletonClassName), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName("Shared"));
                    break;
                }
            }

            return SyntaxFactory.CastExpression(ResolverBuilder.TTypeSyntax, SyntaxFactory.CastExpression(ResolverBuilder.ObjectTypeSyntax, SyntaxFactory.ParenthesizedExpression(objectExpression)));
        }

        private string FindName(string prefix)
        {
            var name = prefix;
            if (!_names.TryGetValue(prefix, out var id))
            {
                _names.Add(prefix, 0);
            }
            else
            {
                _names[prefix] = id + 1;
                name = name + id;
            }

            return name;
        }
    }
}