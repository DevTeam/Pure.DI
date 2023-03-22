namespace Pure.DI.Core.CSharp;

internal class RootPropertiesBuilder: IBuilder<CompositionCode, CompositionCode>
{
    private const string MethodImplOptions = "[System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)0x300)]";

    public CompositionCode Build(CompositionCode composition, CancellationToken cancellationToken)
    {
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        var roots = composition.Roots
            .OrderByDescending(i => i.IsPublic)
            .ThenBy(i => i.Node.Binding.Id)
            .ThenBy(i => i.PropertyName);

        foreach (var root in roots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (membersCounter > 0)
            {
                code.AppendLine();
            }

            code.AppendLines(BuildProperty(root.Injection.Type, root));
            membersCounter++;
        }

        return composition with { MembersCount = membersCounter };
    }
    
    private static ImmutableArray<Line> BuildProperty(ITypeSymbol type, Root root)
    {
        var code = new LinesBuilder();
        code.AppendLine($"{(root.IsPublic ? "public" : "private")} {type} {root.PropertyName}");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine(MethodImplOptions);
            code.AppendLine("get");
            code.AppendLine("{");
            using (code.Indent())
            {
                code.AppendLines(root.Lines);
            }

            code.AppendLine("}");
        }

        code.AppendLine("}");
        return code.Lines.ToImmutableArray();
    }
}