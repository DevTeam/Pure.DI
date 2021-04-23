namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface ITypeResolver
    {
        Dependency Resolve(SemanticType dependency, ExpressionSyntax? tag, ImmutableArray<Location> resolveLocations, bool anyTag = false, bool probe = false);

        IEnumerable<Dependency> Resolve(SemanticType dependency);
    }
}