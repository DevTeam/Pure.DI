// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code;

using System.Text.RegularExpressions;
using System.Xml.Linq;

sealed class Comments : IComments
{
    private const string XmlCommentPrefix = "\u001Fxml:";
    private static readonly Regex CommentRegex = new(@"^\s*(///?)\s*(.+)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex HintCommentRegex = new($@"\s*({string.Join("|", Enum.GetNames(typeof(Hint)))})\s*=\s*(.+)\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public IEnumerable<string> GetComments(IEnumerable<SyntaxTrivia> comments) =>
        from trivia in comments
        from comment in trivia.ToFullString().Split(["\r\n", "\r", "\n"], StringSplitOptions.None)
        select comment.Trim()
        into comment
        select CommentRegex.Match(comment)
        into match
        where match.Success
        let commentValue = match.Groups[2].Value
        where commentValue.Any(char.IsLetterOrDigit)
        select match.Groups[1].Value.Length == 3 ? $"{XmlCommentPrefix}{commentValue}" : commentValue;

    public IEnumerable<string> FilterHints(IEnumerable<string> comments) =>
        comments.Where(i => !HintCommentRegex.IsMatch(GetText(i)));

    public IHints GetHints(IEnumerable<string> comments)
    {
        var hints =
            from comment in comments
            select HintCommentRegex.Match(GetText(comment))
            into match
            where match.Success
            select match;

        var result = new Hints();
        foreach (var hint in hints)
        {
            if (!Enum.TryParse(hint.Groups[1].Value, true, out Hint setting))
            {
                continue;
            }

            result.GetOrAdd(setting, _ => new LinkedList<string>()).AddLast(hint.Groups[2].Value);
        }

        return result;
    }

    public IEnumerable<string> Format(IEnumerable<string> comments, bool escape)
    {
        var allComments = comments.ToList();
        var count = allComments.Count;
        for (var i = 0; i < count; i++)
        {
            var comment = GetText(allComments[i]);
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

    public bool IsXml(IEnumerable<string> comments) =>
        comments.Any(IsXml);

    public IEnumerable<string> FormatXml(IEnumerable<string> comments) =>
        comments.Select(comment => $"/// {UnmarkXml(comment)}");

    public string GetText(string comment) =>
        UnmarkXml(comment);

    public string Escape(string text) => new XText(text).ToString();

    public IEnumerable<string> FormatList(
        string title,
        IEnumerable<(IReadOnlyCollection<string> terms, IReadOnlyCollection<string> decriptions)> items,
        bool escape)
    {
        yield return "/// <para>";
        yield return $"/// {title}<br/>";
        yield return "/// <list type=\"bullet\">";
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

    private static bool IsXml(string comment) =>
        comment.StartsWith(XmlCommentPrefix, StringComparison.Ordinal);

    private static string UnmarkXml(string comment) =>
        IsXml(comment) ? comment[XmlCommentPrefix.Length..] : comment;
}
