namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    internal interface IGeneratorTargets
    {
        IReadOnlyCollection<SyntaxTree> Trees { get; }
    }
}
