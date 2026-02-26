// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable IdentifierTypo

namespace Pure.DI.Core;

sealed class ImplementationVariantsBuilder(
    IVariator<ImplementationVariant> implementationVariator,
    CancellationToken cancellationToken)
    : IBuilder<DpImplementation, IEnumerable<DpImplementation>>
{
    [SuppressMessage("ReSharper", "NotDisposedResourceIsReturned")]
    public IEnumerable<DpImplementation> Build(DpImplementation implementation)
    {
        var setsOfOptions =
            implementation.Methods.Select(method => CreateOptions(method, ImplementationVariantKind.Method))
                .Concat(Enumerable.Repeat(CreateOptions(implementation.Constructor, ImplementationVariantKind.Ctor), 1))
                .Select(i => new SetOfOptions<ImplementationVariant>(i.ToList()))
                .ToList();

        while (implementationVariator.TryGetNext(setsOfOptions, out var implementationOptions))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return implementationOptions.Aggregate(
                implementation with { Methods = ImmutableArray<DpMethod>.Empty },
                (current, variant) => variant.Kind switch
                {
                    ImplementationVariantKind.Ctor => current with { Constructor = variant.Method },
                    ImplementationVariantKind.Method => current with { Methods = current.Methods.Add(variant.Method) },
                    _ => current
                });
        }
    }

    private static IEnumerable<ImplementationVariant> CreateOptions(DpMethod method, ImplementationVariantKind kind)
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