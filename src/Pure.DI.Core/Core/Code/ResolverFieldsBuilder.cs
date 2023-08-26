// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class ResolversFieldsBuilder: IBuilder<CompositionCode, CompositionCode>
{
    private readonly IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>> _resolversBuilder;
    internal static readonly string BucketsFieldName = $"_buckets{Names.Salt}";
    internal static readonly string BucketSizeFieldName = $"_bucketSize{Names.Salt}";

    public ResolversFieldsBuilder(
        IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>> resolversBuilder)
    {
        _resolversBuilder = resolversBuilder;
    }

    public CompositionCode Build(CompositionCode composition)
    {
        if (composition.Source.Source.Hints.GetHint(Hint.Resolve, SettingState.On) != SettingState.On)
        {
            return composition;
        }
        
        var resolvers = _resolversBuilder.Build(composition.Roots).ToArray();
        if (!resolvers.Any())
        {
            return composition;
        }

        var code = composition.Code;
        if (composition.MembersCount > 0)
        {
            code.AppendLine();
        }

        code.AppendLine($"private readonly static int {BucketSizeFieldName};");
        
        var pairs = $"{Names.SystemNamespace}Type, {ResolverClassesBuilder.ResolverInterfaceName}<{composition.Source.Source.Name.ClassName}, object>";
        var pairTypeName = $"{Names.ApiNamespace}Pair<{pairs}>";
        code.AppendLine($"private readonly static {pairTypeName}[] {BucketsFieldName};");
        
        return composition with { MembersCount = composition.MembersCount + 2 };
    }
}