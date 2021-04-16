﻿// ReSharper disable RedundantNameQualifier
namespace Pure.DI.Core
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using Components;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static class SyntaxRepo
    {
        public const string SharedContextName = "SharedContext";
        public const string ContextClassName = "Context";
        public const string FactoriesTableName = "Resolvers";
        public const string FactoriesByTagTableName = "ResolversByTag";
        public static readonly TypeSyntax TTypeSyntax = SyntaxFactory.ParseTypeName("T");
        public static readonly TypeSyntax TypeTypeSyntax = SyntaxFactory.ParseTypeName(typeof(Type).ToString());
        public static readonly TypeSyntax ObjectTypeSyntax = SyntaxFactory.ParseTypeName("object");
        public static readonly SyntaxToken KeyValuePairTypeToken = SyntaxFactory.Identifier("Pure.DI.Components.Pair");
        public static readonly TypeSyntax TagTypeTypeSyntax = SyntaxFactory.ParseTypeName(typeof(TagKey).ToString());
        public static readonly SyntaxToken FuncTypeToken = SyntaxFactory.Identifier("System.Func");
        public static readonly TypeSyntax FuncTypeObjectObjectTypeSyntax = SyntaxFactory.GenericName("System.Func").AddTypeArgumentListArguments(TypeTypeSyntax, ObjectTypeSyntax, ObjectTypeSyntax);
        public static readonly TypeSyntax ResolversTableTypeSyntax = SyntaxFactory.ParseTypeName(typeof(ResolversTable).ToString());
        public static readonly TypeSyntax ResolversWithTagTableTypeSyntax = SyntaxFactory.ParseTypeName(typeof(ResolversByTagTable).ToString());
        public static readonly TypeSyntax ContextTypeSyntax = SyntaxFactory.ParseTypeName(ContextClassName);
        public static readonly TypeSyntax IContextTypeSyntax = SyntaxFactory.ParseTypeName(typeof(IContext).ToString());
        private static readonly TypeParameterSyntax TTypeParameterSyntax = SyntaxFactory.TypeParameter("T");

        public static readonly AttributeSyntax AggressiveInliningAttr = SyntaxFactory.Attribute(
            SyntaxFactory.IdentifierName(nameof(MethodImplAttribute)),
    SyntaxFactory.AttributeArgumentList()
            .AddArguments(
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.CastExpression(
                            SyntaxFactory.ParseTypeName(nameof(MethodImplOptions)),
                            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0x300))))));

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
    }
}
