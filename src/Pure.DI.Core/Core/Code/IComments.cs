namespace Pure.DI.Core.Code;

interface IComments
{
    IEnumerable<string> GetComments(IEnumerable<SyntaxTrivia> comments);

    IEnumerable<string> FilterHints(IEnumerable<string> comments);

    IHints GetHints(IEnumerable<string> comments);

    string Escape(string text);

    IEnumerable<string> Format(IEnumerable<string> comments, bool escape);

    IEnumerable<string> FormatList(
        string title,
        IEnumerable<(IReadOnlyCollection<string> terms, IReadOnlyCollection<string> decriptions)> items,
        bool escape);
}