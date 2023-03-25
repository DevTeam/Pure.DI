namespace Pure.DI.Core.CSharp;

internal class ResolversFieldsBuilder: IBuilder<CompositionCode, CompositionCode>
{
    internal static readonly string BucketsFieldName = $"_buckets{Variable.Postfix}";
    internal static readonly string BucketsByTagFieldName = $"_bucketsByTag{Variable.Postfix}";

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

        code.AppendLine("#region Buckets");
        var rootsWitTags = actualRoots.Where(i => i.Injection.Tag is { }).ToArray();
        if (rootsWitTags.Any())
        {
            CreateMap(composition, rootsWitTags, code, BucketsByTagFieldName);
            code.AppendLine();
        }

        var roots = actualRoots.Where(i => i.Injection.Tag is not { }).ToArray();
        if (roots.Any())
        {
            CreateMap(composition, roots, code, BucketsFieldName);
        }

        code.AppendLine("#endregion");
        return composition with { MembersCount = composition.MembersCount + 2 };
    }

    private static void CreateMap(CompositionCode composition, IReadOnlyCollection<Root> actualRoots, LinesBuilder code, string fieldName)
    {
        var divisor = Buckets<object, object>.GetDivisor((uint)actualRoots.Count);
        var pairs = $"Pure.DI.RootKey, System.Func<{composition.ClassName}, object>";
        var bucketsTypeName = $"Pure.DI.Buckets<{pairs}>";
        var pairTypeName = $"Pure.DI.Pair<{pairs}>";
        code.AppendLine($"private readonly static {pairTypeName}[] {fieldName} = {bucketsTypeName}.{nameof(Buckets<object, object>.Create)}(");
        using (code.Indent())
        {
            code.AppendLine($"{divisor},");
            code.AppendLine($"new {pairTypeName}[{actualRoots.Count}]");
            code.AppendLine("{");
            using (code.Indent())
            {
                bool isFirst = true;
                foreach (var root in actualRoots.OrderBy(i => i.Index))
                {
                    var key = $"new Pure.DI.RootKey(typeof({root.Injection.Type}), {root.Injection.Tag.TagToString()})";
                    var value = $"composition => (object)composition.{root.PropertyName}";
                    code.AppendLine($"{(isFirst ? ' ' : ',')}new {pairTypeName}({key}, {value})");
                    isFirst = false;
                }
            }

            code.AppendLine("});");
        }
    }
}