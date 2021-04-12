namespace Pure.DI.Core
{
    using System;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class BindingLifetimeStrategy : ILifetimeStrategy
    {
        private readonly IBuildContext _buildContext;
        private readonly IDiagnostic _diagnostic;
        private readonly Func<IBuildStrategy> _buildStrategy;

        public BindingLifetimeStrategy(
            IBuildContext buildContext,
            IDiagnostic diagnostic,
            [Tag(Tags.SimpleBuildStrategy)] Func<IBuildStrategy> buildStrategy)
        {
            _buildContext = buildContext;
            _diagnostic = diagnostic;
            _buildStrategy = buildStrategy;
        }

        public Lifetime Lifetime => Lifetime.Binding;

        public ExpressionSyntax Build(TypeDescription typeDescription, ExpressionSyntax objectBuildExpression)
        {
            var resolvedType = typeDescription.Type;
            var classParts = resolvedType.ToMinimalDisplayParts(_buildContext.SemanticModel, 0).Where(i => i.Kind == SymbolDisplayPartKind.ClassName).Select(i => i.ToString());
            var memberKey = new MemberKey(
                string.Join("_", classParts) + "__Lifetime__",
                resolvedType,
                typeDescription.Tag);

            var lifetimeField = (FieldDeclarationSyntax)_buildContext.GetOrAddMember(memberKey, () =>
            {
                var lifetimeContractType = _buildContext.SemanticModel.Compilation.GetTypeByMetadataName("Pure.DI.ILifetime`1")?.Construct(resolvedType);
                if (lifetimeContractType == null)
                {
                    _diagnostic.Error(Diagnostics.CannotResolveLifetime, $"Cannot resolve a lifetime for {resolvedType}.");
                    throw Diagnostics.ErrorShouldTrowException;
                }

                var lifetimeTypeDescription = _buildContext.TypeResolver.Resolve(lifetimeContractType, typeDescription.Tag);
                if (!lifetimeTypeDescription.IsResolved)
                {
                    _diagnostic.Error(Diagnostics.CannotResolveLifetime, $"Cannot find a lifetime for {resolvedType}. Please add a binding for {lifetimeContractType}, for example .Bind<ILifetime<{resolvedType}>>().To<MyLifetime<{resolvedType}>>().");
                    throw Diagnostics.ErrorShouldTrowException;
                }

                var lifetimeObject = _buildStrategy().Build(lifetimeTypeDescription);
                var lifetimeFieldName = _buildContext.NameService.FindName(memberKey);
                return SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(lifetimeTypeDescription.Type.ToTypeSyntax(_buildContext.SemanticModel))
                            .AddVariables(
                                SyntaxFactory.VariableDeclarator(lifetimeFieldName)
                                    .WithInitializer(SyntaxFactory.EqualsValueClause(lifetimeObject))
                            )
                    )
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                        SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
            });

            var lifetimeIdentifier = SyntaxFactory.IdentifierName(lifetimeField.Declaration.Variables.First().Identifier);
            var lambda = SyntaxFactory.ParenthesizedLambdaExpression(objectBuildExpression);
            return SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, lifetimeIdentifier, SyntaxFactory.IdentifierName(nameof(ILifetime<object>.Resolve))))
                .AddArgumentListArguments(SyntaxFactory.Argument(lambda));
        }
    }
}