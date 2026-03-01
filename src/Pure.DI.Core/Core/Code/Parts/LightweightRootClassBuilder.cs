namespace Pure.DI.Core.Code.Parts;

class LightweightRootClassBuilder(
    ITypeResolver typeResolver)
    : IClassPartBuilder
{
    private const int MaxFuncArgumentCount = 16;

    public ClassPart Part => ClassPart.LightweightRootClass;

    public CompositionCode Build(CompositionCode composition)
    {
        var roots = composition.PublicRoots
            .Where(i => i.Kind.HasFlag(RootKinds.Light))
            .Where(i => i.TypeDescription.TypeArgs.Count == 0)
            .Where(i => i.RootArgs.Length <= MaxFuncArgumentCount)
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
                var argTypes = root.RootArgs
                    .Select(arg => $"{typeResolver.Resolve(composition.Source.Source, arg.InstanceType)}, ");
                code.AppendLine($"[{Names.OrdinalAttributeName}()] public {Names.FuncTypeName}<{string.Concat(argTypes)}{rootType}> {root.Source.UniqueName};");
            }
        }
        code.AppendLine("#pragma warning restore CS0649");

        return composition with { MembersCount = membersCount };
    }
}
