// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code;

internal sealed class ResolversFieldsBuilder(IBuilder<RootContext, IEnumerable<ResolverInfo>> resolversBuilder)
    : IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition)
    {
        if (!composition.Source.Source.Hints.IsResolveEnabled)
        {
            return composition;
        }

        var resolvers = resolversBuilder.Build(new RootContext(composition.Source.Source, composition.Roots)).ToList();
        if (resolvers.Count == 0)
        {
            return composition;
        }

        var code = composition.Code;
        code.AppendLine($"private readonly static int {Names.BucketSizeFieldName};");
        var pairs = $"{Names.SystemNamespace}Type, {Names.IResolverTypeName}<{composition.Source.Source.Name.ClassName}, object>";
        var pairTypeName = $"{Names.ApiNamespace}Pair<{pairs}>";
        code.AppendLine($"private readonly static {pairTypeName}[] {Names.BucketsFieldName};");

        return composition with { MembersCount = composition.MembersCount + 2 };
    }
}