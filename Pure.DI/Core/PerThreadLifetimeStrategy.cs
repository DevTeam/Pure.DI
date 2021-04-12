namespace Pure.DI.Core
{
    using System.Linq;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class PerThreadLifetimeStrategy : ILifetimeStrategy
    {
        private readonly IBuildContext _buildContext;

        public PerThreadLifetimeStrategy(IBuildContext buildContext) =>
            _buildContext = buildContext;

        public Lifetime Lifetime => Lifetime.PerThread;

        public ExpressionSyntax Build(TypeDescription typeDescription, ExpressionSyntax objectBuildExpression)
        {
            var resolvedType = typeDescription.Type;
            var classParts = resolvedType.ToMinimalDisplayParts(_buildContext.SemanticModel, 0).Where(i => i.Kind == SymbolDisplayPartKind.ClassName).Select(i => i.ToString());
            var memberKey = new MemberKey(
                string.Join("_", classParts) + "__PerThread__",
                resolvedType,
                typeDescription.Tag);

            var perThreadField = (FieldDeclarationSyntax)_buildContext.GetOrAddMember(memberKey, () =>
            {
                var type = resolvedType.ToTypeSyntax(_buildContext.SemanticModel);
                var threadLocalType = SyntaxFactory.GenericName("System.Threading.ThreadLocal").AddTypeArgumentListArguments(type);
                var lambda = SyntaxFactory.ParenthesizedLambdaExpression(objectBuildExpression);
                var threadLocalObject = SyntaxFactory.ObjectCreationExpression(threadLocalType).AddArgumentListArguments(SyntaxFactory.Argument(lambda));
                var threadSingletonFieldName = _buildContext.NameService.FindName(memberKey);
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