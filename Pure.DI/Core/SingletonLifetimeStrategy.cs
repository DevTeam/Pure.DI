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

        public SingletonLifetimeStrategy(IBuildContext buildContext) =>
            _buildContext = buildContext;

        public Lifetime Lifetime => Lifetime.Singleton;

        public ExpressionSyntax Build(Dependency dependency, ExpressionSyntax objectBuildExpression)
        {
            var resolvedDependency = _buildContext.TypeResolver.Resolve(dependency.Implementation, dependency.Tag, ImmutableArray.Create(objectBuildExpression.GetLocation()));
            var classKey = new MemberKey($"Singleton{resolvedDependency.Implementation}", dependency);
            var singletonClass = _buildContext.GetOrAddMember(classKey, () =>
            {
                var singletonClassName = _buildContext.NameService.FindName(classKey);
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