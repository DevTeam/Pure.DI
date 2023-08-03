// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable IdentifierTypo
namespace Pure.DI.Core;

internal sealed class ImplementationVariantsBuilder: IBuilder<DpImplementation, IEnumerable<DpImplementation>>
{
    private readonly IVariator<ImplementationVariant> _implementationVariator;
    private readonly CancellationToken _cancellationToken;

    public ImplementationVariantsBuilder(
        IVariator<ImplementationVariant> implementationVariator,
        CancellationToken cancellationToken)
    {
        _implementationVariator = implementationVariator;
        _cancellationToken = cancellationToken;
    }

    public IEnumerable<DpImplementation> Build(DpImplementation implementation)
    {
        var variations =
            implementation.Methods.Select(method => CreateVariations(method, ImplementationVariantKind.Method))
            .Concat(Enumerable.Repeat(CreateVariations(implementation.Constructor, ImplementationVariantKind.Ctor), 1))
            .Select(i => i.GetEnumerator())
            .ToArray();

        try
        {
            while (_implementationVariator.TryGetNextVariants(variations, _ => true, out var variants))
            {
                _cancellationToken.ThrowIfCancellationRequested();
                yield return variants.Aggregate(
                    implementation with { Methods = ImmutableArray<DpMethod>.Empty },
                    (current, variant) => variant.Kind switch 
                    {
                        ImplementationVariantKind.Ctor => current with { Constructor = variant.Method },
                        ImplementationVariantKind.Method => current with { Methods = current.Methods.Add(variant.Method) },
                        _ => current
                    });
            }
        }
        finally
        {
            foreach (var variation in variations)
            {
                variation.Dispose();
            }
        }
    }
    
    private static IEnumerable<ImplementationVariant> CreateVariations(DpMethod method, ImplementationVariantKind kind)
    {
        yield return new ImplementationVariant(kind, method);
        
        for (var i = method.Parameters.Length - 1; i >= 0; i--)
        {
            if (!method.Parameters[i].ParameterSymbol.IsOptional)
            {
                yield break;
            }

            yield return new ImplementationVariant(kind, method with { Parameters = method.Parameters.Take(i).ToImmutableArray() });
        }
    }
}