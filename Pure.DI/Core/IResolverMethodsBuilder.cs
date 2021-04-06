namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IResolverMethodsBuilder
    {
        IEnumerable<MemberDeclarationSyntax> CreateResolveMethods();
    }
}