// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable HeapView.DelegateAllocation
namespace Pure.DI.Core;

internal class ApiBuilder : IBuilder<Unit, IEnumerable<Source>>
{
    private readonly IResources _resources;

    public ApiBuilder(IResources resources) => _resources = resources;

    public IEnumerable<Source> Build(Unit data, CancellationToken cancellationToken)
    {
        foreach (var resource in _resources.GetResource("""^[\w\.]+\.g\.cs$"""))
        {
            using (resource)
            {
                yield return new Source(
                    resource.Name,
                    SourceText.From(
                        resource.Content,
                        default,
                        SourceHashAlgorithm.Sha1,
                        false,
                        true));
            }
        }
    }
}