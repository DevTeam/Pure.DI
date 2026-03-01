namespace Pure.DI.Core.Code.Parts;

class LightweightRootClassBuilder(
    ITypeResolver typeResolver)
    : IClassPartBuilder
{
    public ClassPart Part => ClassPart.LightweightRootClass;

    public CompositionCode Build(CompositionCode composition)
    {
        var roots = composition.PublicRoots
            .Where(i => i.Kind.HasFlag(RootKinds.Light))
            .ToList();
        if (roots.Count == 0)
        {
            return composition;
        }

        var membersCount = composition.MembersCount;
        var code = composition.Code;

        code.AppendLine("#pragma warning disable CS0649");
        code.AppendLine($"private sealed class {Names.LightweightRootClassName}: {Names.LightweightRootBaseClassName}");
        using (code.CreateBlock())
        {
            foreach (var root in roots)
            {
                var mdRoot = root.Source;
                var rootType = typeResolver.Resolve(composition.Source.Source, mdRoot.RootType);
                code.AppendLine($"[{Names.OrdinalAttributeName}()] public {Names.FuncTypeName}<{rootType}> {root.Source.UniqueName};");
            }
        }
        code.AppendLine("#pragma warning restore CS0649");

        return composition with { MembersCount = membersCount };
    }
}
