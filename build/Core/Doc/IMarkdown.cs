namespace Build.Core.Doc;

using System.Xml.Linq;

public interface IMarkdown
{
    Task ConvertAsync(
        XDocument document,
        TextWriter markdownWriter,
        Predicate<DocumentTypeName> filter,
        CancellationToken cancellationToken);
}