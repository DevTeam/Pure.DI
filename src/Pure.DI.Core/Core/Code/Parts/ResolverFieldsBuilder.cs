// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code.Parts;

sealed class ResolversFieldsBuilder(IBuilder<RootsContext, IEnumerable<ResolverInfo>> resolversBuilder)
    : IClassPartBuilder
{
    public ClassPart Part => ClassPart.ResolversFields;

    public CompositionCode Build(CompositionCode composition)
    {
        if (!composition.Source.Source.Hints.IsResolveEnabled)
        {
            return composition;
        }

        var resolvers = resolversBuilder.Build(new RootsContext(composition.Source.Source, composition.PublicRoots)).ToList();
        if (resolvers.Count == 0)
        {
            return composition;
        }

        var code = composition.Code;
        code.AppendLine($"private readonly static uint {Names.BucketSizeFieldName};");
        var pairs = $"{Names.IResolverTypeName}<{composition.Source.Source.Name.ClassName}, object>";
        var pairTypeName = $"{Names.ApiNamespace}Pair<{pairs}>";
        code.AppendLine($"private readonly static {pairTypeName}[] {Names.BucketsFieldName};");

        return composition with { MembersCount = composition.MembersCount + 2 };
    }
}