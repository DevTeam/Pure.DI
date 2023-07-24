// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class UnboundTypeConstructor : IUnboundTypeConstructor
{
    public ITypeSymbol Construct(Compilation compilation, ITypeSymbol type) =>
        type switch
        {
            INamedTypeSymbol namedTypeSymbol => namedTypeSymbol.IsGenericType ? namedTypeSymbol.ConstructUnboundGenericType() : namedTypeSymbol,
            IArrayTypeSymbol arrayTypeSymbol => compilation.CreateArrayTypeSymbol(Construct(compilation, arrayTypeSymbol.ElementType)),
            _ => type
        };
}