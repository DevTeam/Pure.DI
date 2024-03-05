// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class ToStringMethodBuilder
    : IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition)
    {
        if (composition.Diagram.IsEmpty)
        {
            return composition;
        }

        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        var hints = composition.Source.Source.Hints;
        var isCommentsEnabled = hints.IsCommentsEnabled;
        if (isCommentsEnabled)
        {
            code.AppendLine("/// <summary>");
            code.AppendLine("/// This method provides a class diagram in mermaid format. To see this diagram, simply call the method and copy the text to this site https://mermaid.live/.");
            code.AppendLine("/// </summary>");
        }

        code.AppendLine("public override string ToString()");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine("return");
            using (code.Indent())
            {
                var lines = composition.Diagram;
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    code.AppendLine(line with { Text = $"\"{line}{(i == lines.Length - 1 ? "\";" : "\\n\" +")}" });
                }
            }
        }
        code.AppendLine("}");
        return composition with { MembersCount = membersCounter };
    }
}