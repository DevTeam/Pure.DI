// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable HeapView.DelegateAllocation
namespace Pure.DI.Core;

internal class ApiBuilder : IBuilder<Unit, IEnumerable<Source>>
{
    private static readonly ImmutableArray<string> ApiTemplates = ImmutableArray.Create(
        "Api.g.cs",
        "GenericTypeArguments.g.cs",
        "Default.g.cs");

    private readonly IResources _resources;

    public ApiBuilder(IResources resources) => _resources = resources;

    public IEnumerable<Source> Build(Unit data, CancellationToken cancellationToken) =>
        from apiTemplate in ApiTemplates
        from resource in _resources.GetResource(apiTemplate)
        select new Source(resource.Name, SourceText.From(resource.Content, default, SourceHashAlgorithm.Sha1, false, true));
}