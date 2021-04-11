namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    internal interface IConstructorsResolver
    {
        IEnumerable<IMethodSymbol> Resolve(TypeDescription typeDescription);
    }
}