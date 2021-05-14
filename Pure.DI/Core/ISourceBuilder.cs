namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.CodeAnalysis;

    internal interface ISourceBuilder
    {
        IEnumerable<Source> Build(Compilation compilation, CancellationToken cancellationToken);
    }
}