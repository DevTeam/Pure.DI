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
        private readonly IBindingResultStrategy _resultStrategy;
        private readonly ICollection<MemberDeclarationSyntax> _additionalMembers;

        public BindingExpressionStrategy(
            SemanticModel semanticModel,
            ITypeResolver typeResolver,
            IBindingResultStrategy resultStrategy,
            ICollection<MemberDeclarationSyntax> additionalMembers)
        {
            _semanticModel = semanticModel;
            _typeResolver = typeResolver;
            _resultStrategy = resultStrategy;
            _additionalMembers = additionalMembers;
        }

        [CanBeNull]
        public ExpressionSyntax TryBuild(
            BindingMetadata binding,
            INamedTypeSymbol contractType,
            ExpressionSyntax tag,
            INameService nameService,
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
                    var singletonClassName = nameService.FindName(string.Join("_", resolvedType.ToMinimalDisplayParts(_semanticModel, 0).Where(i => i.Kind == SymbolDisplayPartKind.ClassName).Select(i => i.ToString())) + "Singleton", contractType, tag);

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

            return _resultStrategy.Build(objectExpression);
        }
    }
}