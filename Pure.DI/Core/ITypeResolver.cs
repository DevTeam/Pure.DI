namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface ITypeResolver
    {
        Dependency Resolve(SemanticType dependency, ExpressionSyntax? tag, bool anyTag = false, bool probe = false);

        IEnumerable<Dependency> Resolve(SemanticType dependency);
    }
}