// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable HeapView.DelegateAllocation
namespace Pure.DI.Core;

internal sealed class ApiBuilder(IResources resources)
    : IBuilder<Unit, IEnumerable<Source>>
{
    public IEnumerable<Source> Build(Unit data)
    {
        foreach (var resource in resources.GetResource("""^[\w\.]+\.g\.cs$"""))
        {
            SourceText source;
            using (resource)
            {
                source = SourceText.From(
                    resource.Content,
                    default,
                    SourceHashAlgorithm.Sha1,
                    false,
                    true);
            }
            
            yield return new Source(resource.Name, source);
        }
    }
}