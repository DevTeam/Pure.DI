// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using NS35EBD81B;

internal class ResolveContextBuilder: IMembersBuilder, IStatementsFinalizer, IArgumentsSupport
{
    private const string CurrentFieldName = "Current";
    private readonly IBuildContext _buildContext;
    private readonly MemberKey _resolveContextClassKey;

    public ResolveContextBuilder(IBuildContext buildContext, IMemberNameService memberNameService)
    {
        _buildContext = buildContext;
        var resolveContextClassName = memberNameService.GetName(MemberNameKind.ResolveContextClass);
        _resolveContextClassKey = new MemberKey(resolveContextClassName, typeof(ResolveContextBuilder));
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
            from arg in GetArgumentsMetadata()
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

        // _prev = Current;
        var prevAssignmentExpression = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName("_prev"),
                SyntaxFactory.IdentifierName(CurrentFieldName)))
            .WithNewLine();
        
        // Current = this;
        var thisAssignmentExpression = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(CurrentFieldName),
                SyntaxFactory.ThisExpression()))
            .WithNewLine();
        
        // Current = _prev;
        var currentDisposeAssignmentExpression = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(CurrentFieldName),
                    SyntaxFactory.IdentifierName("_prev")))
            .WithNewLine();
        
        var assignmentStatements = GetArgumentsMetadata()
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
                .AddMembers(prevVar, currentVar, ctor, dispose)
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
                .AddArgumentListArguments(GetArgumentsMetadata()
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
    
    public IEnumerable<ArgumentMetadata> GetArgumentsMetadata() => 
        _buildContext.Metadata.Arguments
            .Select((arg, index) => new ArgumentMetadata(arg.Type, $"val{arg.Name}_{index}", arg.Tags));

    public IEnumerable<ParameterSyntax> GetParameters() =>
        from arg in GetArgumentsMetadata()
        select SyntaxRepo.Parameter(SyntaxFactory.Identifier(arg.Name)).WithType(arg.Type);
    
    public IEnumerable<ArgumentSyntax> GetArguments() => 
        GetArgumentsMetadata()
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
        return SyntaxRepo.MemberAccess(resolveContextClassName, CurrentFieldName, $"{arg.Name}Arg");
    }
}