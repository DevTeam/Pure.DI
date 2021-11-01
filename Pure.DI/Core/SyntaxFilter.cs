// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core
{
    using System;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class SyntaxFilter : ISyntaxFilter
    {
        private static readonly Type[] Types = {
            typeof(BaseTypeDeclarationSyntax),
            typeof(BaseMethodDeclarationSyntax),
            typeof(BasePropertyDeclarationSyntax),
            typeof(BaseFieldDeclarationSyntax)
        };

        public IComparable Order => 0;

        public bool Accept(SyntaxNode node) => Types.Any(i => node.GetType().IsInstanceOfType(node));
    }
}