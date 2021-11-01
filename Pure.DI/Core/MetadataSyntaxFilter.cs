// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class MetadataSyntaxFilter: ISyntaxFilter
    {
        private static readonly ISet<string> Names = new HashSet<string>
        {
            nameof(DI.Setup),
            nameof(IConfiguration.DependsOn),
            nameof(IConfiguration.Bind),
            nameof(IConfiguration.Default),
            nameof(IConfiguration.OrderAttribute),
            nameof(IConfiguration.TagAttribute),
            nameof(IConfiguration.TypeAttribute),
            nameof(IBinding.As),
            nameof(IBinding.Bind),
            nameof(IBinding.Tags),
            nameof(IBinding.To),
            nameof(IBinding.AnyTag),
            nameof(IBinding)
        };
        
        public IComparable Order => 1;

        public bool Accept(SyntaxNode node)
        {
            if (node is not InvocationExpressionSyntax invocation)
            {
                return false;
            }

            var names = new HashSet<string>();
            foreach (var descendantNode in invocation.Expression.DescendantNodes())
            {
                switch (descendantNode)
                {
                    case IdentifierNameSyntax identifierNameSyntax:
                        names.Add(identifierNameSyntax.Identifier.Text);
                        break;
                    
                    case GenericNameSyntax genericNameSyntax:
                        names.Add(genericNameSyntax.Identifier.Text);
                        break;
                }
            }
            
            return names.Overlaps(Names);
        }
    }
}