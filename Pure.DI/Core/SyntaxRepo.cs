// ReSharper disable RedundantNameQualifier
namespace Pure.DI.Core
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static class SyntaxRepo
    {
        public const string SharedContextName = "SharedContext";
        public const string ContextClassName = "Context";
        public static readonly TypeSyntax TTypeSyntax = SyntaxFactory.ParseTypeName("T");
        public static readonly TypeSyntax ContextTypeSyntax = SyntaxFactory.ParseTypeName(ContextClassName);
        public static readonly TypeSyntax IContextTypeSyntax = SyntaxFactory.ParseTypeName("Pure.DI.IContext");
        public static readonly TypeSyntax ObjectTypeSyntax = SyntaxFactory.ParseTypeName("object");
        private static readonly TypeSyntax TypeTypeSyntax = SyntaxFactory.ParseTypeName("System.Type");
        private static readonly TypeParameterSyntax TTypeParameterSyntax = SyntaxFactory.TypeParameter("T");

        public static readonly AttributeSyntax AggressiveOptimizationAndInliningAttr = SyntaxFactory.Attribute(
            SyntaxFactory.IdentifierName(nameof(MethodImplAttribute)),
    SyntaxFactory.AttributeArgumentList()
            .AddArguments(
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.CastExpression(
                            SyntaxFactory.ParseTypeName(nameof(MethodImplOptions)),
                            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0x100 | 0x200))))));

        public static readonly MethodDeclarationSyntax TResolveMethodSyntax =
            SyntaxFactory.MethodDeclaration(TTypeSyntax, nameof(IContext.Resolve))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(AggressiveOptimizationAndInliningAttr))
                .AddTypeParameterListParameters(TTypeParameterSyntax);

        public static readonly MethodDeclarationSyntax GenericStaticResolveMethodSyntax =
            TResolveMethodSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));

        public static readonly MethodDeclarationSyntax GenericStaticResolveWithTagMethodSyntax =
            GenericStaticResolveMethodSyntax.AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("tag")).WithType(ObjectTypeSyntax));

        private static readonly MethodDeclarationSyntax ObjectResolveMethodSyntax =
            SyntaxFactory.MethodDeclaration(ObjectTypeSyntax, nameof(IContext.Resolve))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(AggressiveOptimizationAndInliningAttr))
                .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("type")).WithType(TypeTypeSyntax));

        public static readonly MethodDeclarationSyntax StaticResolveMethodSyntax =
            ObjectResolveMethodSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));

        public static readonly MethodDeclarationSyntax StaticResolveWithTagMethodSyntax =
            StaticResolveMethodSyntax.AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("tag")).WithType(ObjectTypeSyntax));
    }
}
