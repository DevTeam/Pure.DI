namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class SingletonLifetimeStrategy : ILifetimeStrategy
    {
        private const string ValueName = "Shared";
        private readonly IBuildContext _buildContext;
        private readonly IDisposeStatementsBuilder _disposeStatementsBuilder;
        private readonly IWrapperStrategy _wrapperStrategy;
        private readonly IStringTools _stringTools;

        public SingletonLifetimeStrategy(
            IBuildContext buildContext,
            IDisposeStatementsBuilder disposeStatementsBuilder,
            IWrapperStrategy wrapperStrategy,
            IStringTools stringTools)
        {
            _buildContext = buildContext;
            _disposeStatementsBuilder = disposeStatementsBuilder;
            _wrapperStrategy = wrapperStrategy;
            _stringTools = stringTools;
        }

        public Lifetime Lifetime => Lifetime.Singleton;

        public ExpressionSyntax Build(SemanticType resolvingType, Dependency dependency, ExpressionSyntax objectBuildExpression)
        {
            var resolvedDependency = _buildContext.TypeResolver.Resolve(dependency.Implementation, dependency.Tag);
            var classKey = new MemberKey($"Singleton{_stringTools.ConvertToTitle(resolvedDependency.Implementation.ToString())}", dependency);
            var singletonClass = _buildContext.GetOrAddMember(classKey, () =>
            {
                var singletonClassName = _buildContext.NameService.FindName(classKey);

                var instance = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(singletonClassName),
                    SyntaxFactory.IdentifierName(ValueName));
                
                _buildContext.AddFinalDisposeStatements(_disposeStatementsBuilder.Build(resolvedDependency.Implementation, instance));

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
                                SyntaxFactory.Token(SyntaxKind.InternalKeyword),
                                SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))
                    );
            });

            var instanceExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ParseName(singletonClass.Identifier.Text), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(ValueName));
            return _wrapperStrategy.Build(resolvingType, dependency, instanceExpression);
        }
    }
}