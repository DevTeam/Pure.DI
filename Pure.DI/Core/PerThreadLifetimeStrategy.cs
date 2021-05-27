namespace Pure.DI.Core
{
    using System.Threading;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class PerThreadLifetimeStrategy : ILifetimeStrategy
    {
        private readonly IBuildContext _buildContext;
        private readonly IDisposeStatementsBuilder _disposeStatementsBuilder;

        public PerThreadLifetimeStrategy(
            IBuildContext buildContext,
            IDisposeStatementsBuilder disposeStatementsBuilder)
        {
            _buildContext = buildContext;
            _disposeStatementsBuilder = disposeStatementsBuilder;
        }

        public Lifetime Lifetime => Lifetime.PerThread;

        public ExpressionSyntax Build(Dependency dependency, ExpressionSyntax objectBuildExpression)
        {
            var resolvedType = dependency.Implementation;
            var memberKey = new MemberKey($"PerThread{dependency.Binding.Implementation}", dependency);
            var perThreadField = (FieldDeclarationSyntax)_buildContext.GetOrAddMember(memberKey, () =>
            {
                var semanticModel = dependency.Implementation.SemanticModel;
                var threadLocalTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName("System.Threading.ThreadLocal`1");
                var genericThreadLocalType = new SemanticType(threadLocalTypeSymbol!, semanticModel);
                var threadLocalType = genericThreadLocalType.Construct(resolvedType);
                var lambda = SyntaxFactory.ParenthesizedLambdaExpression(objectBuildExpression);
                var threadLocalObject = SyntaxFactory.ObjectCreationExpression(threadLocalType).AddArgumentListArguments(SyntaxFactory.Argument(lambda));
                var threadSingletonFieldName = _buildContext.NameService.FindName(memberKey);
                
                _buildContext.AddReleaseStatements(_disposeStatementsBuilder.Build(threadLocalType, SyntaxFactory.IdentifierName(threadSingletonFieldName)));
                
                return SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(threadLocalType)
                            .AddVariables(
                                SyntaxFactory.VariableDeclarator(threadSingletonFieldName)
                                    .WithInitializer(SyntaxFactory.EqualsValueClause(threadLocalObject))
                            )
                    )
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                        SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
            });

            var perThreadFieldIdentifier = SyntaxFactory.IdentifierName(perThreadField.Declaration.Variables.First().Identifier);
            return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, perThreadFieldIdentifier, SyntaxFactory.IdentifierName(nameof(ThreadLocal<object>.Value)));
        }
    }
}