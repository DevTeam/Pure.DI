// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using NS35EBD81B;

internal class ResolveContextMembersBuilder: IMembersBuilder, IStatementsFinalizer, IArgumentsSupport
{
    private const string CurrentFieldName = "_current";
    private const string CurrentPropertyName = "Current";
    private readonly IBuildContext _buildContext;
    private readonly MemberKey _resolveContextClassKey;

    public ResolveContextMembersBuilder(IBuildContext buildContext, IMemberNameService memberNameService)
    {
        _buildContext = buildContext;
        var resolveContextClassName = memberNameService.GetName(MemberNameKind.ResolveContextClass);
        _resolveContextClassKey = new MemberKey(resolveContextClassName, typeof(ResolveContextMembersBuilder));
    }

    public int Order => 0;
    
    private bool IsActive => _buildContext.Metadata.Arguments.Any();

    public IEnumerable<MemberDeclarationSyntax> BuildMembers(SemanticModel semanticModel)
    {
        // ResolveUsingCurrentContext(Type type)
        yield return
            SyntaxRepo.MethodDeclaration(SyntaxRepo.ObjectTypeSyntax, "ResolveUsingCurrentContext")
                .AddModifiers(SyntaxKind.PrivateKeyword.WithSpace())
                .AddModifiers(SyntaxKind.StaticKeyword.WithSpace())
                .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxRepo.AggressiveInliningAttr))
                .AddParameterListParameters(SyntaxRepo.Parameter(SyntaxFactory.Identifier("type")).WithType(SyntaxRepo.TypeTypeSyntax))
                .AddBodyStatements(
                    SyntaxRepo.ReturnStatement()
                        .WithExpression(
                            SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(nameof(IContext.Resolve)))
                                .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("type")))
                                .AddArgumentListArguments(GetArguments().ToArray())));
        
        if (!IsActive)
        {
            yield break;   
        }

        var fields = 
            from arg in _buildContext.Metadata.Arguments
            let variableDeclaration = SyntaxFactory
                .VariableDeclaration(arg.Type.TypeSyntax)
                .AddSpace()
                .AddVariables(SyntaxFactory.VariableDeclarator($"{arg.Name}Arg"))
            select 
                SyntaxFactory.FieldDeclaration(variableDeclaration)
                .AddModifiers(SyntaxKind.PublicKeyword.WithSpace())
                .WithNewLine();

        var resolveContextClassName = _buildContext.NameService.FindName(_resolveContextClassKey);
        
        var prevVar = SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName(resolveContextClassName))
                    .AddSpace()
                    .AddVariables(SyntaxFactory.VariableDeclarator("_prev")))
            .AddModifiers(SyntaxKind.PrivateKeyword.WithSpace())
            .WithNewLine();

        var currentVar = SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName(resolveContextClassName))
                    .AddSpace()
                    .AddVariables(SyntaxFactory.VariableDeclarator(CurrentFieldName)))
            .AddModifiers(SyntaxKind.PublicKeyword.WithSpace())
            .AddModifiers(SyntaxKind.StaticKeyword.WithSpace())
            .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxRepo.ThreadStaticAttr).AddSpace())
            .WithNewLine();

        var checkCurrentFieldStatement = SyntaxFactory.IfStatement(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.EqualsExpression,
                SyntaxFactory.IdentifierName(CurrentFieldName),
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
            SyntaxRepo.ThrowStatement(SyntaxRepo.ObjectCreationExpression(
                    SyntaxFactory.ParseTypeName("System.InvalidOperationException"))
                .AddArgumentListArguments(SyntaxFactory.Argument("Arguments are not available with delayed resolution (in cases like Func outside constructors, IServiceCollection, etc.), they can only be used in the static composition object graph.".ToLiteralExpression()!))));
        
        var returnCurrentFieldStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(CurrentFieldName).WithSpace()).WithNewLine();
        var contextType = SyntaxFactory.ParseTypeName(resolveContextClassName);
        var currentProperty = SyntaxFactory.PropertyDeclaration(contextType.AddSpace(), CurrentPropertyName)
            .AddModifiers(SyntaxKind.PublicKeyword.WithSpace())
            .AddModifiers(SyntaxKind.StaticKeyword.WithSpace())
            .AddAccessorListAccessors(
                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithNewLine()
                    .AddBodyStatements(checkCurrentFieldStatement, returnCurrentFieldStatement).WithNewLine(),
                SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithNewLine()
                    .AddBodyStatements(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName(CurrentFieldName), SyntaxFactory.IdentifierName("value")))))
            .WithNewLine();

        // _prev = _current;
        var prevAssignmentExpression = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName("_prev"),
                SyntaxFactory.IdentifierName(CurrentFieldName)))
            .WithNewLine();
        
        // _current = this;
        var thisAssignmentExpression = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(CurrentFieldName),
                SyntaxFactory.ThisExpression()))
            .WithNewLine();
        
        // _current = _prev;
        var currentDisposeAssignmentExpression = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(CurrentFieldName),
                    SyntaxFactory.IdentifierName("_prev")))
            .WithNewLine();
        
        var assignmentStatements = _buildContext.Metadata.Arguments
            .Select(arg => (StatementSyntax)SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName($"{arg.Name}Arg"),
                SyntaxFactory.IdentifierName(arg.Name)).WithNewLine()));

        var ctor = 
            SyntaxFactory.ConstructorDeclaration(resolveContextClassName)
                .AddModifiers(SyntaxKind.PublicKeyword.WithSpace())
                .WithBody(
                    SyntaxFactory.Block()
                        .AddStatements(prevAssignmentExpression, thisAssignmentExpression)
                        .AddStatements(assignmentStatements.ToArray()))
                .AddParameterListParameters(GetParameters().ToArray())
                .WithNewLine();
        
        var dispose = 
            SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)).AddSpace(), nameof(IDisposable.Dispose))
                .AddModifiers(SyntaxKind.PublicKeyword.WithSpace())
                .WithBody(SyntaxFactory.Block().AddStatements(currentDisposeAssignmentExpression))
                .WithNewLine();
        
        yield return
            SyntaxRepo.ClassDeclaration(resolveContextClassName)
                .AddModifiers(SyntaxKind.PrivateKeyword.WithSpace())
                .AddModifiers(SyntaxKind.SealedKeyword.WithSpace())
                .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxRepo.DisposableTypeSyntax))
                .AddMembers(prevVar, currentVar, currentProperty, ctor, dispose)
                .AddMembers(fields.Cast<MemberDeclarationSyntax>().ToArray())
                .WithNewLine()
                .WithPragmaWarningDisable(0169);
    }
    
    public BlockSyntax? AddFinalizationStatements(BlockSyntax? block)
    {
        if (block == default || !IsActive)
        {
            return block;
        }
        
        var resolveContextClassName = _buildContext.NameService.FindName(_resolveContextClassKey);
        var contextType = SyntaxFactory.ParseTypeName(resolveContextClassName);

        // new ResolveContext()
        var createContextExpression =
            SyntaxFactory.ObjectCreationExpression(contextType.WithSpace())
                .AddArgumentListArguments(_buildContext.Metadata.Arguments
                    .Select(argument => SyntaxFactory.Argument(SyntaxFactory.IdentifierName($"{argument.Name}"))).ToArray());

        // ResolveContext resolveContext = new ResolveContext();
        var resolveContextInitStatement = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(contextType).AddVariables(
                SyntaxFactory.VariableDeclarator("resolveContext")
                    .WithSpace()
                    .WithInitializer(SyntaxFactory.EqualsValueClause(createContextExpression))))
            .WithNewLine();
        
        // resolveContext.Dispose();
        var disposeStatement = SyntaxRepo.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("resolveContext"),
                    SyntaxFactory.IdentifierName(nameof(IDisposable.Dispose)))
            ).AddArgumentListArguments())
            .WithNewLine();
        
        var finallyBlock = SyntaxFactory.Block().AddStatements(disposeStatement);

        var tryStatement = SyntaxFactory.TryStatement(
            block,
            SyntaxFactory.List<CatchClauseSyntax>(),
            SyntaxFactory.FinallyClause(finallyBlock));

        return
            SyntaxFactory.Block()
                .AddStatements(resolveContextInitStatement)
                .AddStatements(tryStatement);
    }
    
    public IEnumerable<ParameterSyntax> GetParameters() =>
        from arg in _buildContext.Metadata.Arguments
        select SyntaxRepo.Parameter(SyntaxFactory.Identifier(arg.Name)).WithType(arg.Type);
    
    public IEnumerable<ArgumentSyntax> GetArguments() => 
        _buildContext.Metadata.Arguments
            .Select(argument => SyntaxFactory.Argument(GetArgumentAccessExpression(argument)));

    public SimpleLambdaExpressionSyntax CreateArgumentFactory(ArgumentMetadata arg) =>
        SyntaxFactory.SimpleLambdaExpression(
            SyntaxRepo.Parameter(SyntaxFactory.Identifier("_")),
            SyntaxFactory.Block()
                .AddStatements(
                    SyntaxFactory.ReturnStatement(GetArgumentAccessExpression(arg).WithSpace())));
    
    private ExpressionSyntax GetArgumentAccessExpression(ArgumentMetadata arg)
    {
        var resolveContextClassName = _buildContext.NameService.FindName(_resolveContextClassKey);
        return SyntaxRepo.MemberAccess(SyntaxFactory.IdentifierName(resolveContextClassName), SyntaxFactory.IdentifierName(CurrentPropertyName), SyntaxFactory.IdentifierName($"{arg.Name}Arg"));
    }
}