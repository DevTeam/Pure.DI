namespace Pure.DI.Core.CSharp;

internal class ResolversFieldsBuilder: IBuilder<CompositionCode, CompositionCode>
{
    internal static readonly string BucketsFieldName = $"_buckets{Variable.Postfix}";

    public CompositionCode Build(CompositionCode composition, CancellationToken cancellationToken)
    {
        var actualRoots = composition.Roots.GetActualRoots().ToArray();
        if (!actualRoots.Any())
        {
            return composition;
        }

        var code = composition.Code;
        if (composition.MembersCount > 0)
        {
            code.AppendLine();
        }

        var pairs = $"System.Type, {ResolverClassesBuilder.ResolverInterfaceName}<{composition.Name.ClassName}>";
        var pairTypeName = $"{CodeConstants.ApiNamespace}Pair<{pairs}>";
        code.AppendLine($"private readonly static {pairTypeName}[] {BucketsFieldName};");
        return composition with { MembersCount = composition.MembersCount + 2 };
    }
}