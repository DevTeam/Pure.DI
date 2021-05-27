namespace Pure.DI.Core
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class SingletonLifetimeStrategy : ILifetimeStrategy
    {
        private const string ValueName = "Shared";
        private readonly IBuildContext _buildContext;
        private readonly IDisposeStatementsBuilder _disposeStatementsBuilder;

        public SingletonLifetimeStrategy(
            IBuildContext buildContext,
            IDisposeStatementsBuilder disposeStatementsBuilder)
        {
            _buildContext = buildContext;
            _disposeStatementsBuilder = disposeStatementsBuilder;
        }

        public Lifetime Lifetime => Lifetime.Singleton;

        public ExpressionSyntax Build(Dependency dependency, ExpressionSyntax objectBuildExpression)
        {
            var resolvedDependency = _buildContext.TypeResolver.Resolve(dependency.Implementation, dependency.Tag, ImmutableArray.Create(objectBuildExpression.GetLocation()));
            var classKey = new MemberKey($"Singleton{resolvedDependency.Implementation}", dependency);
            var singletonClass = _buildContext.GetOrAddMember(classKey, () =>
            {
                var singletonClassName = _buildContext.NameService.FindName(classKey);

                var instance = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(singletonClassName),
                    SyntaxFactory.IdentifierName(ValueName));
                
                _buildContext.AddReleaseStatements(_disposeStatementsBuilder.Build(resolvedDependency.Implementation, instance));

                return SyntaxFactory.ClassDeclaration(singletonClassName)
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                    .AddMembers(
                        SyntaxFactory.FieldDeclaration(
                                SyntaxFactory.VariableDeclaration(resolvedDependency.Implementation)
                                    .AddVariables(
                                        SyntaxFactory.VariableDeclarator(ValueName)
                                            .WithInitializer(SyntaxFactory.EqualsValueClause(objectBuildExpression))
                                    )
                            )
                            .AddModifiers(
                                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))
                    );
            });

            return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ParseName(((ClassDeclarationSyntax)singletonClass).Identifier.Text), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(ValueName));
        }
    }
}