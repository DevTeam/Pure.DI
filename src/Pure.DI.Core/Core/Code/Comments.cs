// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

using System.Text.RegularExpressions;

internal class Comments : IComments
{
    private static readonly Regex CommentRegex = new(@"//\s*(.+)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex HintCommentRegex = new($@"\s*({string.Join("|", Enum.GetNames(typeof(Hint)))})\s*=\s*(.+)\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public IEnumerable<string> GetComments(IEnumerable<SyntaxTrivia> comments) =>
        from trivia in comments
        select trivia.ToFullString().Trim()
        into comment
        select CommentRegex.Match(comment)
        into match
        where match.Success
        let commentValue = match.Groups[1].Value
        where commentValue.Any(char.IsLetterOrDigit)
        select commentValue;

    public IEnumerable<string> FilterHints(IEnumerable<string> comments) => 
        comments.Where(i => !HintCommentRegex.IsMatch(i));

    public IHints GetHints(IEnumerable<string> comments)
    {
        var hints = (
                from comment in comments
                select HintCommentRegex.Match(comment)
                into match
                where match.Success
                select match)
            .ToArray();

        var result = new Hints();
        foreach (var hint in hints)
        {
            if (!Enum.TryParse(hint.Groups[1].Value, true, out Hint setting))
            {
                continue;
            }

            result[setting] = hint.Groups[2].Value;
        }

        return result;
    }
    
    public IEnumerable<string> Format(IEnumerable<string> comments, bool escape)
    {
        var allComments = comments.ToArray();
        var count = allComments.Length;
        for (var i = 0; i < count; i++)
        {
            var comment = allComments[i];
            if (escape)
            {
                comment = Escape(comment);
            }

            if (i < count - 1)
            {
                yield return $"/// {comment}<br/>";
            }
            else
            {
                yield return $"/// {comment}";
            }
        }
    }
    
    public string Escape(string text) => new System.Xml.Linq.XText(text).ToString();

    public IEnumerable<string> FormatList(
        string title,
        IEnumerable<(IReadOnlyCollection<string> terms, IReadOnlyCollection<string> decriptions)> items,
        bool escape)
    {
        yield return "/// <para>";
        yield return $"/// {title}<br/>";
        yield return "/// <list type=\"table\">";
        yield return "/// <listheader>";
        yield return "/// <term>Root</term>";
        yield return "/// <description>Description</description>";
        yield return "/// </listheader>";
        foreach (var (terms, descriptions) in items)
        {
            yield return "/// <item>";
            
            yield return "/// <term>";
            foreach (var term in Format(terms, escape))
            {
                yield return term;
            }
            yield return "/// </term>";
            
            yield return "/// <description>";
            foreach (var description in Format(descriptions, escape))
            {
                yield return description;
            }
            yield return "/// </description>";
            yield return "/// </item>";
        }
                
        yield return "/// </list>";
        yield return "/// </para>";
    }
}