namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    internal interface ITypeResolver
    {
        IReadOnlyCollection<BindingMetadata> AdditionalBindings { get; }

        INamedTypeSymbol Resolve(INamedTypeSymbol typeSymbol);
    }
}