namespace Pure.DI.Core.Code;

internal class RootMethodsCommenter(IComments comments): ICommenter<Root>
{
    public void AddComments(CompositionCode composition, Root root)
    {
        if (!composition.Source.Source.Hints.IsCommentsEnabled)
        {
            return;
        }
        
        var rootComments = root.Source.Comments;
        if (rootComments.Count <= 0)
        {
            return;
        }

        var code = composition.Code;
        code.AppendLine("/// <summary>");
        foreach (var comment in comments.Format(rootComments))
        {
            code.AppendLine(comment);
        }

        code.AppendLine("/// </summary>");
    }
}