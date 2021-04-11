namespace Pure.DI.Core
{
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class BindingExpressionStrategy : IBindingExpressionStrategy
    {
        private readonly IBuildContext _buildContext;
        private readonly ITracer _tracer;
        private readonly ITypeResolver _typeResolver;
        private readonly IBindingResultStrategy _resultStrategy;
        private readonly IBindingExpressionStrategy _dependencyBindingExpressionStrategy;
        
        public BindingExpressionStrategy(
            IBuildContext buildContext,
            ITracer tracer,
            ITypeResolver typeResolver,
            IBindingResultStrategy resultStrategy,
            IBindingExpressionStrategy? dependencyBindingExpressionStrategy = null)
        {
            _buildContext = buildContext;
            _tracer = tracer;
            _typeResolver = typeResolver;
            _resultStrategy = resultStrategy;
            _dependencyBindingExpressionStrategy = dependencyBindingExpressionStrategy ?? this;
        }

        public ExpressionSyntax TryBuild(TypeResolveDescription typeResolveDescription)
        {
            using var traceToken = _tracer.RegisterResolving(typeResolveDescription);
            var objectExpression = typeResolveDescription.ObjectBuilder.TryBuild(_dependencyBindingExpressionStrategy, typeResolveDescription);
            switch (typeResolveDescription.Binding.Lifetime)
            {
                case Lifetime.Singleton:
                    {
                        var resolvedType = typeResolveDescription.Type;
                        var classParts = resolvedType.ToMinimalDisplayParts(_buildContext.SemanticModel, 0).Where(i => i.Kind == SymbolDisplayPartKind.ClassName).Select(i => i.ToString());
                        var memberKey = new MemberKey(
                            string.Join("_", classParts) + "Singleton",
                            resolvedType,
                            typeResolveDescription.Tag);

                        var expression = objectExpression;
                        var singletonClass = _buildContext.GetOrAddMember(memberKey, () =>
                        {
                            var singletonClassName = _buildContext.NameService.FindName(memberKey);
                            return SyntaxFactory.ClassDeclaration(singletonClassName)
                                .AddModifiers(
                                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                                    SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                                .AddMembers(
                                    SyntaxFactory.FieldDeclaration(
                                            SyntaxFactory.VariableDeclaration(
                                                    resolvedType.ToTypeSyntax(_buildContext.SemanticModel))
                                                .AddVariables(
                                                    SyntaxFactory.VariableDeclarator("Shared")
                                                        .WithInitializer(SyntaxFactory.EqualsValueClause(expression))
                                                )
                                        )
                                        .AddModifiers(
                                            SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                            SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                                            SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))
                                );
                        });

                        objectExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ParseName(((ClassDeclarationSyntax)singletonClass).Identifier.Text), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName("Shared"));
                        break;
                    }
            }

            return _resultStrategy.Build(objectExpression);
        }

        public ExpressionSyntax TryBuild(ITypeSymbol contractType, ExpressionSyntax? tag) =>
            TryBuild(_typeResolver.Resolve(contractType, tag));
    }
}