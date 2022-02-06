// ReSharper disable RedundantNameQualifier
namespace Pure.DI.Core;

using System.Runtime.CompilerServices;
using NS35EBD81B;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class SyntaxRepo
{
    private const string DisposeSingletonsMethodName = "FinalDispose";
    private const string GetResolverMethodName = "GetResolver";
    public const string OnDisposableEventName = "OnDisposable";
    public const string RaiseOnDisposableMethodName = "RaiseOnDisposable";
    private static readonly TypeSyntax VoidTypeSyntax = SyntaxFactory.ParseTypeName("void");
    public static readonly TypeSyntax BoolTypeSyntax = SyntaxFactory.ParseTypeName("bool");
    private static readonly TypeSyntax DisposableTypeSyntax = SyntaxFactory.ParseTypeName(typeof(IDisposable).ToString());
    public static readonly TypeSyntax TTypeSyntax = SyntaxFactory.ParseTypeName("T");
    private static readonly TypeSyntax LifetimeTypeSyntax = SyntaxFactory.ParseTypeName(typeof(Lifetime).ToString());
    public static readonly TypeSyntax TypeTypeSyntax = SyntaxFactory.ParseTypeName(typeof(Type).ToString());
    public static readonly TypeSyntax UIntTypeSyntax = SyntaxFactory.ParseTypeName(typeof(uint).ToString());
    public static readonly TypeSyntax ObjectTypeSyntax = SyntaxFactory.ParseTypeName("object");
    public static readonly TypeSyntax TagTypeTypeSyntax = SyntaxFactory.ParseTypeName(typeof(TagKey).ToString());
    private static readonly SyntaxToken FuncTypeToken = SyntaxFactory.Identifier("System.Func");
    public static readonly TypeSyntax FuncOfObjectTypeSyntax = SyntaxFactory.GenericName(FuncTypeToken).AddTypeArgumentListArguments(ObjectTypeSyntax);
    public static readonly TypeParameterSyntax TTypeParameterSyntax = SyntaxFactory.TypeParameter("T");

    public static readonly AttributeSyntax ThreadStaticAttr = SyntaxFactory.Attribute(
        SyntaxFactory.IdentifierName($"System.{nameof(ThreadStaticAttribute)}"));

    public static readonly AttributeSyntax AggressiveInliningAttr = SyntaxFactory.Attribute(
        SyntaxFactory.IdentifierName(typeof(MethodImplAttribute).FullName),
        SyntaxFactory.AttributeArgumentList()
            .AddArguments(
                SyntaxFactory.AttributeArgument(
                    SyntaxFactory.CastExpression(
                        SyntaxFactory.ParseTypeName(typeof(MethodImplOptions).FullName),
                        SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0x100))))));

    public static readonly MethodDeclarationSyntax TResolveMethodSyntax =
        SyntaxFactory.MethodDeclaration(TTypeSyntax, nameof(IContext.Resolve))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(AggressiveInliningAttr))
            .AddTypeParameterListParameters(TTypeParameterSyntax);

    public static readonly MethodDeclarationSyntax GenericStaticResolveMethodSyntax =
        TResolveMethodSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));

    public static readonly MethodDeclarationSyntax GenericStaticResolveWithTagMethodSyntax =
        GenericStaticResolveMethodSyntax.AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("tag")).WithType(ObjectTypeSyntax));

    private static readonly MethodDeclarationSyntax ObjectResolveMethodSyntax =
        SyntaxFactory.MethodDeclaration(ObjectTypeSyntax, nameof(IContext.Resolve))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(AggressiveInliningAttr))
            .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("type")).WithType(TypeTypeSyntax));

    public static readonly MethodDeclarationSyntax StaticResolveMethodSyntax =
        ObjectResolveMethodSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));

    public static readonly MethodDeclarationSyntax StaticResolveWithTagMethodSyntax =
        StaticResolveMethodSyntax.AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("tag")).WithType(ObjectTypeSyntax));

    public static readonly MethodDeclarationSyntax FinalDisposeMethodSyntax =
        SyntaxFactory.MethodDeclaration(VoidTypeSyntax, DisposeSingletonsMethodName)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
            .AddParameterListParameters();

    public static readonly MethodDeclarationSyntax GetResolverMethodSyntax =
        SyntaxFactory.MethodDeclaration(FuncOfObjectTypeSyntax, GetResolverMethodName)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
            .AddParameterListParameters()
            .AddTypeParameterListParameters(TTypeParameterSyntax);

    public static readonly MethodDeclarationSyntax RaiseOnDisposableMethodSyntax =
        SyntaxFactory.MethodDeclaration(TTypeSyntax, RaiseOnDisposableMethodName)
            .AddParameterListParameters(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("disposable")).WithType(TTypeSyntax),
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("lifetime")).WithType(LifetimeTypeSyntax))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
            .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(AggressiveInliningAttr))
            .AddTypeParameterListParameters(TTypeParameterSyntax)
            .AddConstraintClauses(SyntaxFactory.TypeParameterConstraintClause(SyntaxFactory.IdentifierName("T")).AddConstraints(SyntaxFactory.TypeConstraint(DisposableTypeSyntax)));

    public static T WithCommentBefore<T>(this T node, params string[] comments) where T : SyntaxNode =>
        node.WithLeadingTrivia(node.GetLeadingTrivia().Concat(SplitLines(comments).Select(SyntaxFactory.Comment)));

    public static T WithPragmaWarningDisable<T>(this T node, params int[] warningNumbers) where T : SyntaxNode =>
        node.WithLeadingTrivia(
            warningNumbers.Aggregate(
                node.GetLeadingTrivia(),
                (current, warningNumber) =>
                    current.Add(
                        SyntaxFactory.Trivia(
                            SyntaxFactory.PragmaWarningDirectiveTrivia(
                                SyntaxFactory.Token(SyntaxKind.DisableKeyword),
                                SyntaxFactory.SeparatedList<ExpressionSyntax>().Add(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        SyntaxFactory.Literal(warningNumber))),
                                false)))));

    private static IEnumerable<string> SplitLines(IEnumerable<string> strings) =>
        from str in strings
        from subStr in str.Split(new[]
        {
            '\r', '\n'
        }, StringSplitOptions.RemoveEmptyEntries)
        select subStr.TrimStart().StartsWith("//") ? subStr : $"// {subStr}";
}