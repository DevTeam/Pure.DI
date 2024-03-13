namespace Pure.DI.Core.Code;

internal class RootMethodsCommenter(
    IFormatter formatter,
    IComments comments)
    : ICommenter<Root>
{
    public void AddComments(CompositionCode composition, Root root)
    {
        if (!composition.Source.Source.Hints.IsCommentsEnabled)
        {
            return;
        }
        
        var rootComments = root.Source.Comments;
        var code = composition.Code;
        code.AppendLine("/// <summary>");
        if (rootComments.Count > 0)
        {
            foreach (var comment in comments.Format(rootComments, true))
            {
                code.AppendLine(comment);
            }
        }
        else
        {
            code.AppendLine($"/// Provides a composition root of type {formatter.FormatRef(root.Node.Type)}.");
        }

        code.AppendLine("/// </summary>");
        if (!root.IsPublic)
        {
            return;
        }
        
        code.AppendLine("/// <example>");
        code.AppendLine($"/// This shows how to get an instance of type {formatter.FormatRef(root.Node.Type)}:");
        code.AppendLine("/// <code>");
        code.AppendLine($"/// {(composition.DisposablesCount == 0 ? "" : "using ")}var composition = new {composition.Source.Source.Name.ClassName}({string.Join(", ", composition.Args.Where(i => i.Node.Arg?.Source.Kind == ArgKind.Class).Select(arg => arg.VariableName))});");
        code.AppendLine($"/// var instance = composition.{formatter.Format(root)};");
        code.AppendLine("/// </code>");
        code.AppendLine("/// </example>");
    }
}