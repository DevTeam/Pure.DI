namespace Pure.DI.Core;

internal class CodeRootPropertiesBuilder: IBuilder<ComposerInfo, ComposerInfo>
{
    private const string MethodImplOptions = "[System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)0x300)]";

    public ComposerInfo Build(ComposerInfo composer, CancellationToken cancellationToken)
    {
        var code = composer.Code;
        var membersCounter = composer.MembersCount;
        var roots = composer.Roots
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

        return composer with { MembersCount = membersCounter };
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