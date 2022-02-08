namespace Pure.DI.Core;

using NS35EBD81B;

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

    public Lifetime? Lifetime => NS35EBD81B.Lifetime.Singleton;

    public ExpressionSyntax Build(SemanticType resolvingType, Dependency dependency, ExpressionSyntax objectBuildExpression)
    {
        var resolvedDependency = _buildContext.TypeResolver.Resolve(dependency.Implementation, dependency.Tag);
        var classKey = new MemberKey($"Singleton{_stringTools.ConvertToTitle(resolvedDependency.Implementation.ToString())}", dependency);
        var singletonClass = _buildContext.GetOrAddMember(classKey, () =>
        {
            var singletonClassName = _buildContext.NameService.FindName(classKey);
            var fieldKey = new MemberKey($"Has{singletonClassName}", dependency);
            var hasSingletonFieldName = _buildContext.NameService.FindName(fieldKey);

            var isDisposable = dependency.Implementation.ImplementsInterface<IDisposable>();

            var singletonClass = SyntaxFactory.ClassDeclaration(singletonClassName)
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

            if (!isDisposable)
            {
                return singletonClass;
            }

            _buildContext.GetOrAddMember(fieldKey, () =>
                SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(SyntaxRepo.BoolTypeSyntax).AddVariables(
                            SyntaxFactory.VariableDeclarator(hasSingletonFieldName)))
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                        SyntaxFactory.Token(SyntaxKind.VolatileKeyword)));

            singletonClass = singletonClass.AddMembers(
                SyntaxFactory.ConstructorDeclaration(singletonClassName)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword)).WithBody(SyntaxFactory.Block().AddStatements(
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                SyntaxFactory.IdentifierName(hasSingletonFieldName),
                                SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)))
                    )));


            var instance = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(singletonClassName),
                SyntaxFactory.IdentifierName(ValueName));

            var hasInstance = SyntaxFactory.IdentifierName(hasSingletonFieldName);
            _buildContext.AddFinalDisposeStatements(_disposeStatementsBuilder.Build(instance, hasInstance));

            return singletonClass;
        });

        var instanceExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ParseName(singletonClass.Identifier.Text), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(ValueName));
        return _wrapperStrategy.Build(resolvingType, dependency, instanceExpression);
    }
}