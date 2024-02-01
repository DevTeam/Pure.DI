// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class ToStringMethodBuilder(IBuilder<CompositionCode, LinesBuilder> classDiagramBuilder)
    : IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition)
    {
        if (composition.Source.Source.Hints.GetHint<SettingState>(Hint.ToString) != SettingState.On)
        {
            return composition;
        }

        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        if (composition.MembersCount > 0)
        {
            code.AppendLine();
        }

        var classDiagram = classDiagramBuilder.Build(composition);
        
        code.AppendLine("public override string ToString()");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine("return");
            using (code.Indent())
            {
                var lines = classDiagram.Lines.ToArray();
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