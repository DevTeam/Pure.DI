namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    internal interface ISourceBuilder
    {
        IEnumerable<Source> Build(Compilation contextCompilation, IEnumerable<SyntaxTree> treesWithMetadata);
    }
}