// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable IdentifierTypo

namespace Pure.DI.Core;

internal sealed class ImplementationVariantsBuilder(
    IVariator<ImplementationVariant> implementationVariator,
    CancellationToken cancellationToken)
    : IBuilder<DpImplementation, IEnumerable<DpImplementation>>
{
    [SuppressMessage("ReSharper", "NotDisposedResourceIsReturned")]
    public IEnumerable<DpImplementation> Build(DpImplementation implementation)
    {
        var variants =
            implementation.Methods.Select(method => CreateVariants(method, ImplementationVariantKind.Method))
                .Concat(Enumerable.Repeat(CreateVariants(implementation.Constructor, ImplementationVariantKind.Ctor), 1))
                .Select(i => new SafeEnumerator<ImplementationVariant>(i.GetEnumerator()))
                .ToList();

        try
        {
            while (implementationVariator.TryGetNextVariants(variants, out var curVariants))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return curVariants.Aggregate(
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
            foreach (var variation in variants)
            {
                variation.Dispose();
            }
        }
    }

    private static IEnumerable<ImplementationVariant> CreateVariants(DpMethod method, ImplementationVariantKind kind)
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