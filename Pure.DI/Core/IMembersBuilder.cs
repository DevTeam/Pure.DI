namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IMembersBuilder
    {
        int Order { get; }

        IEnumerable<MemberDeclarationSyntax> BuildMembers(SemanticModel semanticModel);
    }
}