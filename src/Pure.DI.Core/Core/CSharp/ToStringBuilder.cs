namespace Pure.DI.Core.CSharp;

internal class ToStringBuilder: IBuilder<CompositionCode, CompositionCode>
{
    private readonly IBuilder<CompositionCode, LinesBuilder> _classDiagramBuilder;

    public ToStringBuilder(IBuilder<CompositionCode, LinesBuilder> classDiagramBuilder) => 
        _classDiagramBuilder = classDiagramBuilder;

    public CompositionCode Build(CompositionCode composition, CancellationToken cancellationToken)
    {
        if (composition.Source.Source.Settings.GetState(Setting.ToString) != SettingState.On)
        {
            return composition;
        }

        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        if (composition.MembersCount > 0)
        {
            code.AppendLine();
        }

        var classDiagram = _classDiagramBuilder.Build(composition, cancellationToken);
        
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
                    code.AppendLine(line with { Text = $"\"{line.Text}{(i == lines.Length - 1 ? "\";" : "\\n\" +")}" });
                }
            }
        }
        code.AppendLine("}");
        return composition with { MembersCount = membersCounter };
    }
}