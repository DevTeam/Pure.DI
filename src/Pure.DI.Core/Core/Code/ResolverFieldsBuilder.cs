// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class ResolversFieldsBuilder: IBuilder<CompositionCode, CompositionCode>
{
    private readonly IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>> _resolversBuilder;

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

        code.AppendLine($"private readonly static int {Names.BucketSizeFieldName};");
        
        var pairs = $"{Names.SystemNamespace}Type, {Names.ResolverInterfaceName}<{composition.Source.Source.Name.ClassName}, object>";
        var pairTypeName = $"{Names.ApiNamespace}Pair<{pairs}>";
        code.AppendLine($"private readonly static {pairTypeName}[] {Names.BucketsFieldName};");
        
        return composition with { MembersCount = composition.MembersCount + 2 };
    }
}