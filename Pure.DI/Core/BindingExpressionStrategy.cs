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
        private readonly INameService _nameService;
        private readonly IDictionary<MemberKey, MemberDeclarationSyntax> _additionalMembers;
        private readonly IBindingExpressionStrategy _dependencyBindingExpressionStrategy;

        public BindingExpressionStrategy(
            SemanticModel semanticModel,
            ITypeResolver typeResolver,
            IBindingResultStrategy resultStrategy,
            INameService nameService,
            IDictionary<MemberKey, MemberDeclarationSyntax> additionalMembers,
            IBindingExpressionStrategy? dependencyBindingExpressionStrategy)
        {
            _semanticModel = semanticModel;
            _typeResolver = typeResolver;
            _resultStrategy = resultStrategy;
            _nameService = nameService;
            _additionalMembers = additionalMembers;
            _dependencyBindingExpressionStrategy = dependencyBindingExpressionStrategy ?? this;
        }

        public ExpressionSyntax TryBuild(
            TypeResolveDescription typeResolveDescription,
            ISet<BindingMetadata> additionalBindings)
        {
            var objectExpression = typeResolveDescription.ObjectBuilder.TryBuild(_typeResolver, _dependencyBindingExpressionStrategy, typeResolveDescription, additionalBindings);
            switch (typeResolveDescription.Binding.Lifetime)
            {
                case Lifetime.Singleton:
                    {
                        var resolvedType = typeResolveDescription.Type;
                        var classParts = resolvedType.ToMinimalDisplayParts(_semanticModel, 0).Where(i => i.Kind == SymbolDisplayPartKind.ClassName).Select(i => i.ToString());
                        var memberKey = new MemberKey(
                            string.Join("_", classParts) + "Singleton",
                            resolvedType,
                            typeResolveDescription.Tag);

                        if (!_additionalMembers.TryGetValue(memberKey, out var singletonClass))
                        {
                            var singletonClassName = _nameService.FindName(memberKey);
                            singletonClass = SyntaxFactory.ClassDeclaration(singletonClassName)
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

                            _additionalMembers.Add(memberKey, singletonClass);
                        }

                        objectExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ParseName(((ClassDeclarationSyntax)singletonClass).Identifier.Text), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName("Shared"));
                        break;
                    }
            }

            return _resultStrategy.Build(objectExpression);
        }

        public ExpressionSyntax TryBuild(ITypeSymbol contractType,
            ExpressionSyntax? tag,
            ISet<BindingMetadata> additionalBindings)
        {
            var typeResolveDescription = _typeResolver.Resolve(contractType, tag);
            return TryBuild(typeResolveDescription, additionalBindings);
        }
    }
}