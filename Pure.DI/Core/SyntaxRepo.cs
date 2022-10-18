// ReSharper disable RedundantNameQualifier
namespace Pure.DI.Core;

using System.Runtime.CompilerServices;
using NS35EBD81B;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class SyntaxRepo
{
    private const string DisposeSingletonsMethodName = "FinalDispose";
    private const string GetResolverMethodName = "GetResolver";
    private const string ResolveInContextMethodName = "ResolveInContext";
    public const string OnDisposableEventName = "OnDisposable";
    public const string RaiseOnDisposableMethodName = "RaiseOnDisposable";
    private static readonly TypeSyntax VoidTypeSyntax = SyntaxFactory.ParseTypeName("void");
    public static readonly TypeSyntax BoolTypeSyntax = SyntaxFactory.ParseTypeName("bool");
    public static readonly TypeSyntax DisposableTypeSyntax = SyntaxFactory.ParseTypeName(typeof(IDisposable).ToString());
    public static readonly TypeSyntax TTypeSyntax = SyntaxFactory.ParseTypeName("T");
    public static readonly TypeSyntax TypeTypeSyntax = SyntaxFactory.ParseTypeName(typeof(Type).ToString());
    public static readonly TypeSyntax UIntTypeSyntax = SyntaxFactory.ParseTypeName(typeof(uint).ToString());
    public static readonly TypeSyntax ObjectTypeSyntax = SyntaxFactory.ParseTypeName("object");
    private static readonly SyntaxToken FuncTypeToken = SyntaxFactory.Identifier("System.Func");
    public static readonly TypeSyntax FuncOfObjectTypeSyntax = SyntaxFactory.GenericName(FuncTypeToken).AddTypeArgumentListArguments(ObjectTypeSyntax);
    public static readonly TypeParameterSyntax TTypeParameterSyntax = SyntaxFactory.TypeParameter("T");

    public static ExpressionSyntax MemberAccess(params IdentifierNameSyntax[] identifierNames)
    {
        if (identifierNames.Length == 0) throw new ArgumentException(nameof(identifierNames));
        return identifierNames.Skip(1)
            .Aggregate(
                (ExpressionSyntax)identifierNames[0],
                (acc, path) => 
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        acc,
                        path));
    }
    
    public static SyntaxToken WithSpace(this SyntaxKind syntaxKind) =>
        SyntaxFactory.Token(SyntaxFactory.TriviaList(), syntaxKind, SyntaxFactory.TriviaList(SyntaxFactory.ElasticSpace));
    
    public static SyntaxToken WithSpace(this SyntaxToken syntaxToken) =>
        syntaxToken.WithLeadingTrivia(syntaxToken.LeadingTrivia.Concat(new []{SyntaxFactory.ElasticSpace}));

    private static SyntaxToken WithNewLine(this SyntaxKind syntaxKind) =>
        SyntaxFactory.Token(SyntaxFactory.TriviaList(), syntaxKind, SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturn, SyntaxFactory.LineFeed));

    public static TSyntax WithSpace<TSyntax>(this TSyntax node) where TSyntax : SyntaxNode => 
        node.WithLeadingTrivia(node.GetLeadingTrivia().Concat(new []{SyntaxFactory.ElasticSpace}));
    
    public static TSyntax AddSpace<TSyntax>(this TSyntax node) where TSyntax : SyntaxNode => 
        node.WithTrailingTrivia(node.GetTrailingTrivia().Concat(new []{SyntaxFactory.ElasticSpace}));

    public static TSyntax WithNewLine<TSyntax>(this TSyntax node) where TSyntax : SyntaxNode =>
        node.WithLeadingTrivia(node.GetLeadingTrivia().Concat(new []{SyntaxFactory.CarriageReturn, SyntaxFactory.LineFeed}));
    
    public static ExpressionStatementSyntax ExpressionStatement(ExpressionSyntax expression)
        => SyntaxFactory.ExpressionStatement(default, expression, SyntaxKind.SemicolonToken.WithNewLine());
    
    public static ObjectCreationExpressionSyntax ObjectCreationExpression(TypeSyntax type, ArgumentListSyntax? argumentList = default, InitializerExpressionSyntax? initializer = default)
        => SyntaxFactory.ObjectCreationExpression(SyntaxKind.NewKeyword.WithSpace(), type, argumentList, initializer);
    
#pragma warning disable RS0027
    public static ReturnStatementSyntax ReturnStatement(ExpressionSyntax? expression = default)
        => SyntaxFactory.ReturnStatement(default, SyntaxKind.ReturnKeyword.WithSpace(), expression, SyntaxFactory.Token(SyntaxKind.SemicolonToken));
#pragma warning restore RS0027
    
    public static ArrayCreationExpressionSyntax ArrayCreationExpression(ArrayTypeSyntax type, InitializerExpressionSyntax? initializer = default)
        => SyntaxFactory.ArrayCreationExpression(SyntaxKind.NewKeyword.WithSpace(), type, initializer);
    
    public static MethodDeclarationSyntax MethodDeclaration(TypeSyntax returnType, string identifier)
        => SyntaxFactory.MethodDeclaration(default, default, returnType, default, SyntaxFactory.Identifier(identifier).WithSpace(), default, SyntaxFactory.ParameterList(), default, default, default, default);
    
    public static MethodDeclarationSyntax MethodDeclaration(TypeSyntax returnType, SyntaxToken identifier)
        => SyntaxFactory.MethodDeclaration(default, default, returnType, default, identifier.WithSpace(), default, SyntaxFactory.ParameterList(), default, default, default, default);
    
    public static ClassDeclarationSyntax ClassDeclaration(string identifier)
        => SyntaxFactory.ClassDeclaration(default, default, SyntaxKind.ClassKeyword.WithSpace(), SyntaxFactory.Identifier(identifier), default, default, default, SyntaxKind.OpenBraceToken.WithNewLine(), default, SyntaxKind.CloseBraceToken.WithNewLine(), default);
    
    public static ParameterSyntax Parameter(SyntaxToken identifier)
        => SyntaxFactory.Parameter(default, default, default, identifier.WithSpace(), default);
    
#pragma warning disable RS0027
    public static ThrowStatementSyntax ThrowStatement(ExpressionSyntax? expression = default)
        => SyntaxFactory.ThrowStatement(default, SyntaxKind.ThrowKeyword.WithSpace(), expression, SyntaxFactory.Token(SyntaxKind.SemicolonToken));
#pragma warning restore RS0027
    
    public static EventFieldDeclarationSyntax EventFieldDeclaration(VariableDeclarationSyntax declaration)
        => SyntaxFactory.EventFieldDeclaration(default, default, SyntaxKind.EventKeyword.WithSpace(), declaration, SyntaxFactory.Token(SyntaxKind.SemicolonToken));

    public static readonly AttributeSyntax ThreadStaticAttr = SyntaxFactory.Attribute(
        SyntaxFactory.IdentifierName($"System.{nameof(ThreadStaticAttribute)}"));

    public static readonly AttributeSyntax AggressiveInliningAttr = SyntaxFactory.Attribute(
        SyntaxFactory.IdentifierName(typeof(MethodImplAttribute).FullName),
        SyntaxFactory.AttributeArgumentList()
            .AddArguments(
                SyntaxFactory.AttributeArgument(
                    SyntaxFactory.CastExpression(
                        SyntaxFactory.ParseTypeName(typeof(MethodImplOptions).FullName),
                        SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(256 + 512))))));

    public static MethodDeclarationSyntax CreateTResolveMethodSyntax(SyntaxKind accessibilityToken) =>
        MethodDeclaration(TTypeSyntax, nameof(IContext.Resolve))
            .AddModifiers(accessibilityToken.WithSpace())
            .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(AggressiveInliningAttr))
            .AddTypeParameterListParameters(TTypeParameterSyntax);

    public static MethodDeclarationSyntax CreateGenericStaticResolveMethodSyntax(SyntaxKind accessibilityToken) =>
        CreateTResolveMethodSyntax(accessibilityToken).AddModifiers(SyntaxKind.StaticKeyword.WithSpace());

    public static MethodDeclarationSyntax CreateGenericStaticResolveWithTagMethodSyntax(SyntaxKind tokens) =>
        CreateGenericStaticResolveMethodSyntax(tokens).AddParameterListParameters(Parameter(SyntaxFactory.Identifier("tag")).WithType(ObjectTypeSyntax));

    private static MethodDeclarationSyntax CreateObjectResolveMethodSyntax(SyntaxKind accessibilityToken) =>
        MethodDeclaration(ObjectTypeSyntax, nameof(IContext.Resolve))
            .AddModifiers(accessibilityToken.WithSpace())
            .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(AggressiveInliningAttr))
            .AddParameterListParameters(Parameter(SyntaxFactory.Identifier("type")).WithType(TypeTypeSyntax));

    public static MethodDeclarationSyntax CreateStaticResolveMethodSyntax(SyntaxKind accessibilityToken) =>
        CreateObjectResolveMethodSyntax(accessibilityToken).AddModifiers(SyntaxKind.StaticKeyword.WithSpace());

    public static MethodDeclarationSyntax CreateStaticResolveWithTagMethodSyntax(SyntaxKind accessibilityToken) =>
        CreateStaticResolveMethodSyntax(accessibilityToken).AddParameterListParameters(Parameter(SyntaxFactory.Identifier("tag")).WithType(ObjectTypeSyntax));
    
#pragma warning disable RS0027
    public static YieldStatementSyntax YieldStatement(SyntaxKind kind, ExpressionSyntax? expression = default)
        => SyntaxFactory.YieldStatement(kind, default, SyntaxKind.YieldKeyword.WithSpace(), SyntaxFactory.Token(GetYieldStatementReturnOrBreakKeywordKind(kind)), expression, SyntaxKind.SemicolonToken.WithNewLine());
#pragma warning restore RS0027
    
    private static SyntaxKind GetYieldStatementReturnOrBreakKeywordKind(SyntaxKind kind)
        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        => kind switch
        {
            SyntaxKind.YieldReturnStatement => SyntaxKind.ReturnKeyword,
            SyntaxKind.YieldBreakStatement => SyntaxKind.BreakKeyword,
            _ => throw new ArgumentOutOfRangeException()
        };

    public static readonly MethodDeclarationSyntax FinalDisposeMethodSyntax =
        MethodDeclaration(VoidTypeSyntax, DisposeSingletonsMethodName)
            .AddModifiers(SyntaxKind.InternalKeyword.WithSpace(), SyntaxKind.StaticKeyword.WithSpace())
            .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(AggressiveInliningAttr))
            .AddParameterListParameters();

    public static readonly MethodDeclarationSyntax GetResolverMethodSyntax =
        MethodDeclaration(FuncOfObjectTypeSyntax, GetResolverMethodName)
            .AddModifiers(SyntaxKind.PrivateKeyword.WithSpace(), SyntaxKind.StaticKeyword.WithSpace())
            .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(AggressiveInliningAttr))
            .AddParameterListParameters()
            .AddTypeParameterListParameters(TTypeParameterSyntax);
    
    public static readonly MethodDeclarationSyntax ResolveInContextMethodSyntax =
        MethodDeclaration(SyntaxFactory.IdentifierName("T"), ResolveInContextMethodName)
            .AddModifiers(SyntaxKind.PrivateKeyword.WithSpace(), SyntaxKind.StaticKeyword.WithSpace())
            .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(AggressiveInliningAttr))
            .AddParameterListParameters(
                Parameter(SyntaxFactory.Identifier("resolver"))
                    .WithType(SyntaxFactory.GenericName(FuncTypeToken).AddTypeArgumentListArguments(SyntaxFactory.IdentifierName("T"))))
            .AddTypeParameterListParameters(TTypeParameterSyntax)
            .AddBodyStatements(
                SyntaxFactory.ReturnStatement(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("resolver").WithSpace()).AddArgumentListArguments()));

    public static MethodDeclarationSyntax RaiseOnDisposableMethodSyntax =>
        MethodDeclaration(TTypeSyntax, RaiseOnDisposableMethodName)
            .AddParameterListParameters(
                Parameter(SyntaxFactory.Identifier("disposable")).WithType(TTypeSyntax),
                Parameter(SyntaxFactory.Identifier("lifetime")).WithType(SyntaxFactory.ParseTypeName(typeof(Lifetime).FullName.ReplaceNamespace())))
            .AddModifiers(SyntaxKind.PrivateKeyword.WithSpace(), SyntaxKind.StaticKeyword.WithSpace())
            .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(AggressiveInliningAttr))
            .AddTypeParameterListParameters(TTypeParameterSyntax);

    public static T WithCommentBefore<T>(this T node, params string[] comments) where T : SyntaxNode =>
        node.WithLeadingTrivia(node.GetLeadingTrivia().Concat(new []{SyntaxFactory.CarriageReturn, SyntaxFactory.LineFeed}).Concat(SplitLines(comments).Select(SyntaxFactory.Comment)).Concat(new []{SyntaxFactory.CarriageReturn, SyntaxFactory.LineFeed}));

    public static T WithPragmaWarningDisable<T>(this T node, params int[] warningNumbers) where T : SyntaxNode =>
        node.WithLeadingTrivia(node.GetLeadingTrivia().Concat(
            warningNumbers.Aggregate(
                node.GetLeadingTrivia(),
                (current, warningNumber) =>
                    current.Add(
                        SyntaxFactory.Trivia(
                            PragmaWarningDirectiveTrivia(
                                SyntaxKind.DisableKeyword,
                                SyntaxFactory.SeparatedList<ExpressionSyntax>().Add(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        SyntaxFactory.Literal(warningNumber))),
                                false))))));
    
    private static PragmaWarningDirectiveTriviaSyntax PragmaWarningDirectiveTrivia(SyntaxKind disableOrRestoreKeyword, SeparatedSyntaxList<ExpressionSyntax> errorCodes, bool isActive)
        => SyntaxFactory.PragmaWarningDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.HashToken), SyntaxKind.PragmaKeyword.WithSpace(), SyntaxKind.WarningKeyword.WithSpace(), disableOrRestoreKeyword.WithSpace(), errorCodes, SyntaxKind.EndOfDirectiveToken.WithNewLine(), isActive);

    private static IEnumerable<string> SplitLines(IEnumerable<string> strings) =>
        from str in strings
        from subStr in str.Split(new[]
        {
            '\r', '\n'
        }, StringSplitOptions.RemoveEmptyEntries)
        select subStr.TrimStart().StartsWith("//") ? subStr : $"// {subStr}";
}