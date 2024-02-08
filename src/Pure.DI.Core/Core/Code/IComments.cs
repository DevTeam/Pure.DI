namespace Pure.DI.Core.Code;

internal interface IComments
{
    IEnumerable<string> GetComments(IEnumerable<SyntaxTrivia> comments);

    IEnumerable<string> FilterHints(IEnumerable<string> comments);

    IHints GetHints(IEnumerable<string> comments);
    
    IEnumerable<string> Format(IEnumerable<string> comments);
    
    IEnumerable<string> FormatList(
        string title,
        IEnumerable<(IReadOnlyCollection<string> terms, IReadOnlyCollection<string> decriptions)> items);
}