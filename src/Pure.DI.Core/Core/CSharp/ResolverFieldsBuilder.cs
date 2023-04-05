namespace Pure.DI.Core.CSharp;

internal class ResolversFieldsBuilder: IBuilder<CompositionCode, CompositionCode>
{
    private readonly IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>> _resolversBuilder;
    internal static readonly string BucketsFieldName = $"_buckets{Variable.Postfix}";

    public ResolversFieldsBuilder(
        IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>> resolversBuilder)
    {
        _resolversBuilder = resolversBuilder;
    }

    public CompositionCode Build(CompositionCode composition, CancellationToken cancellationToken)
    {
        if (composition.Source.Source.Settings.GetState(Setting.Resolve, SettingState.On) != SettingState.On)
        {
            return composition;
        }
        
        var resolvers = _resolversBuilder.Build(composition.Roots, cancellationToken).ToArray();
        if (!resolvers.Any())
        {
            return composition;
        }

        var code = composition.Code;
        if (composition.MembersCount > 0)
        {
            code.AppendLine();
        }

        var pairs = $"System.Type, {ResolverClassesBuilder.ResolverInterfaceName}<{composition.Name.ClassName}>";
        var pairTypeName = $"{Constant.ApiNamespace}Pair<{pairs}>";
        code.AppendLine($"private readonly static {pairTypeName}[] {BucketsFieldName};");
        return composition with { MembersCount = composition.MembersCount + 2 };
    }
}