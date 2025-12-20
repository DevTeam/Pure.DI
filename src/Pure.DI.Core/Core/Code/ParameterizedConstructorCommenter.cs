namespace Pure.DI.Core.Code;

sealed class ParameterizedConstructorCommenter(
    IComments comments,
    IFormatter formatter) : ICommenter<Unit>
{
    public void AddComments(CompositionCode composition, Unit unit)
    {
        if (!composition.Source.Source.Hints.IsCommentsEnabled)
        {
            return;
        }

        var code = composition.Code;
        code.AppendLine("/// <summary>");
        code.AppendLine($"/// This parameterized constructor creates a new instance of <see cref=\"{composition.Source.Source.Name.ClassName}\"/> with arguments.");
        code.AppendLine("/// </summary>");
        foreach (var arg in composition.ClassArgs.GetArgsOfKind(ArgKind.Composition))
        {
            if (arg.Node.Arg?.Source is not {} mdArg)
            {
                continue;
            }

            if (mdArg.Comments.Count > 0)
            {
                code.AppendLine($"/// <param name=\"{mdArg.ArgName}\">");
                foreach (var comment in comments.Format(mdArg.Comments, true))
                {
                    code.AppendLine(comment);
                }

                code.AppendLine("/// </param>");
            }
            else
            {
                code.AppendLine($"/// <param name=\"{mdArg.ArgName}\">The composition argument of type {formatter.FormatRef(mdArg.Type)}.</param>");
            }
        }
    }
}