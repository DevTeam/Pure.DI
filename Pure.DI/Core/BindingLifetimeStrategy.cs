namespace Pure.DI.Core
{
    using System;
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

        public ExpressionSyntax Build(Dependency dependency, ExpressionSyntax objectBuildExpression)
        {
            var resolvedType = dependency.Implementation;
            var memberKey = new MemberKey($"Lifetime{dependency.Binding.Implementation}", dependency);
            var lifetimeField = (FieldDeclarationSyntax)_buildContext.GetOrAddMember(memberKey, () =>
            {
                var lifetimeDependencyType = resolvedType.SemanticModel.Compilation.GetTypeByMetadataName("Pure.DI.ILifetime`1")?.Construct(resolvedType.Type);
                if (lifetimeDependencyType == null)
                {
                    var error = $"Cannot resolve a lifetime for {resolvedType}.";
                    _diagnostic.Error(Diagnostics.Error.CannotResolveLifetime, error);
                    throw new HandledException(error);
                }

                var lifetimeTypeDescription = _buildContext.TypeResolver.Resolve(new SemanticType(lifetimeDependencyType, resolvedType), dependency.Tag, dependency.Implementation.Type.Locations);
                if (!lifetimeTypeDescription.IsResolved)
                {
                    var error = $"Cannot find a lifetime for {resolvedType}. Please add a binding for {lifetimeDependencyType}, for example .Bind<ILifetime<{resolvedType}>>().To<MyLifetime<{resolvedType}>>().";
                    _diagnostic.Error(Diagnostics.Error.CannotResolveLifetime, error);
                    throw new HandledException(error);
                }

                var lifetimeObject = _buildStrategy().Build(lifetimeTypeDescription);
                var lifetimeFieldName = _buildContext.NameService.FindName(memberKey);
                return SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(lifetimeTypeDescription.Implementation.TypeSyntax)
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