namespace Pure.DI.Core
{
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class PerResolveLifetimeStrategy : ILifetimeStrategy
    {
        private readonly IBuildContext _buildContext;

        public PerResolveLifetimeStrategy(IBuildContext buildContext) =>
            _buildContext = buildContext;

        public Lifetime Lifetime => Lifetime.PerResolve;

        public ExpressionSyntax Build(Dependency dependency, ExpressionSyntax objectBuildExpression)
        {
            var resolvedType = dependency.Implementation;
            var fieldKey = new MemberKey($"PerResolve{dependency.Binding.Implementation}", dependency);
            var fieldType = resolvedType.Type.IsReferenceType
                ? resolvedType.TypeSyntax
                : SyntaxRepo.ObjectTypeSyntax;

            var perResolveField = (FieldDeclarationSyntax) _buildContext.GetOrAddMember(fieldKey, () =>
            {
                var resolveInstanceFieldName = _buildContext.NameService.FindName(fieldKey);
                return SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(fieldType)
                            .AddVariables(
                                SyntaxFactory.VariableDeclarator(resolveInstanceFieldName)
                            )
                    )
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword));
            });

            var lockObjectKey = new MemberKey($"PerResolveLockObject{dependency.Binding.Implementation}", dependency);
            var lockObjectField = (FieldDeclarationSyntax)_buildContext.GetOrAddMember(lockObjectKey, () =>
            {
                var lockObjectFieldName = _buildContext.NameService.FindName(lockObjectKey);
                return SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(SyntaxRepo.ObjectTypeSyntax)
                            .AddVariables(
                                SyntaxFactory.VariableDeclarator(lockObjectFieldName)
                                    .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.ObjectCreationExpression(SyntaxRepo.ObjectTypeSyntax).AddArgumentListArguments()))
                            )
                    )
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                        SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword));
            });

            var methodKey = new MemberKey($"GetPerResolve{dependency.Binding.Implementation}", dependency);
            var factoryMethod = (MethodDeclarationSyntax)_buildContext.GetOrAddMember(methodKey, () =>
            {
                var factoryName = _buildContext.NameService.FindName(methodKey);
                var type = resolvedType.TypeSyntax;
                var method = SyntaxFactory.MethodDeclaration(type, SyntaxFactory.Identifier(factoryName))
                    .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxRepo.AggressiveInliningAttr))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.PrivateKeyword));

                var resolveInstanceFieldIdentifier = SyntaxFactory.IdentifierName(perResolveField.Declaration.Variables.First().Identifier);
                ExpressionSyntax fieldExpression = resolvedType.Type.IsReferenceType
                    ? resolveInstanceFieldIdentifier 
                    : SyntaxFactory.CastExpression(type, resolveInstanceFieldIdentifier);
                var returnStatement = SyntaxFactory.ReturnStatement(fieldExpression);

                var assignmentBlock = SyntaxFactory.Block()
                    .AddStatements(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, resolveInstanceFieldIdentifier, objectBuildExpression)))
                    .AddStatements(returnStatement);

                var check = SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, resolveInstanceFieldIdentifier, SyntaxFactory.DefaultExpression(fieldType));
                var lockObject = SyntaxFactory.IdentifierName(lockObjectField.Declaration.Variables.First().Identifier);
                var ifStatement = SyntaxFactory.IfStatement(
                    check,
                    SyntaxFactory.LockStatement(
                        lockObject,
                        SyntaxFactory.IfStatement(
                            check,
                            assignmentBlock
                        )
                    ));

                _buildContext.AddFinalizationStatement(
                    SyntaxFactory.LockStatement(
                        lockObject, 
                        SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, resolveInstanceFieldIdentifier, SyntaxFactory.DefaultExpression(fieldType)))));

                return method
                    .AddBodyStatements(ifStatement)
                    .AddBodyStatements(returnStatement);
            });
            
            return SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(factoryMethod.Identifier));
        }
    }
}