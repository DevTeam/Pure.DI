namespace Pure.DI.Core;

// ReSharper disable once ClassNeverInstantiated.Global
internal class PerResolveLifetimeStrategy : ILifetimeStrategy
{
    private readonly IBuildContext _buildContext;
    private readonly IRaiseOnDisposableExpressionBuilder _raiseOnDisposableExpressionBuilder;
    private readonly IWrapperStrategy _wrapperStrategy;
    private readonly IStringTools _stringTools;

    public PerResolveLifetimeStrategy(
        IBuildContext buildContext,
        IRaiseOnDisposableExpressionBuilder raiseOnDisposableExpressionBuilder,
        IWrapperStrategy wrapperStrategy,
        IStringTools stringTools)
    {
        _buildContext = buildContext;
        _raiseOnDisposableExpressionBuilder = raiseOnDisposableExpressionBuilder;
        _wrapperStrategy = wrapperStrategy;
        _stringTools = stringTools;
    }

    public Lifetime Lifetime => Lifetime.PerResolve;

    public ExpressionSyntax Build(SemanticType resolvingType, Dependency dependency, ExpressionSyntax objectBuildExpression)
    {
        var methodKey = new MemberKey($"GetPerResolve{_stringTools.ConvertToTitle(dependency.Binding.Implementation?.ToString() ?? string.Empty)}", dependency);
        var factoryMethod = _buildContext.GetOrAddMember(methodKey, () =>
        {
            var resolvedType = dependency.Implementation;
            var fieldKey = new MemberKey($"_perResolve{_stringTools.ConvertToTitle(dependency.Binding.Implementation?.ToString() ?? string.Empty)}", dependency);
            var fieldType = resolvedType.Type.IsReferenceType
                ? resolvedType.TypeSyntax
                : SyntaxRepo.ObjectTypeSyntax;

            if ((_buildContext.Compilation.Options.NullableContextOptions & NullableContextOptions.Enable) == NullableContextOptions.Enable)
            {
                fieldType = SyntaxFactory.NullableType(fieldType);
            }

            var perResolveField = _buildContext.GetOrAddMember(fieldKey, () =>
            {
                var resolveInstanceFieldName = _buildContext.NameService.FindName(fieldKey);
                return SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(fieldType)
                            .AddVariables(
                                SyntaxFactory.VariableDeclarator(resolveInstanceFieldName)))
                    .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxRepo.ThreadStaticAttr))
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword));
            });

            var factoryName = _buildContext.NameService.FindName(methodKey);
            var type = resolvedType.TypeSyntax;
            var method = SyntaxFactory.MethodDeclaration(type, SyntaxFactory.Identifier(factoryName))
                .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxRepo.AggressiveInliningAttr))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword));

            var resolveInstanceFieldIdentifier = SyntaxFactory.IdentifierName(perResolveField.Declaration.Variables.First().Identifier);
            ExpressionSyntax fieldExpression = resolvedType.Type.IsReferenceType
                ? resolveInstanceFieldIdentifier
                : SyntaxFactory.CastExpression(type, resolveInstanceFieldIdentifier);

            var returnStatement = SyntaxFactory.ReturnStatement(fieldExpression);

            objectBuildExpression = _raiseOnDisposableExpressionBuilder.Build(dependency.Implementation, Lifetime.PerResolve, objectBuildExpression);

            var assignmentBlock = SyntaxFactory.Block()
                .AddStatements(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, resolveInstanceFieldIdentifier, objectBuildExpression)))
                .AddStatements(returnStatement);

            var check = SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, resolveInstanceFieldIdentifier, SyntaxFactory.DefaultExpression(fieldType));
            var ifStatement = SyntaxFactory.IfStatement(check, assignmentBlock);

            _buildContext.AddFinalizationStatements(
                new[]
                {
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, resolveInstanceFieldIdentifier, SyntaxFactory.DefaultExpression(fieldType)))
                });

            return method
                .AddBodyStatements(ifStatement)
                .AddBodyStatements(returnStatement);
        });

        var instanceExpression = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(factoryMethod.Identifier));
        return _wrapperStrategy.Build(resolvingType, dependency, instanceExpression);
    }
}