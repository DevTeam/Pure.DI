namespace Build.Core.Doc;

using System.Xml.Linq;

[ExcludeFromCodeCoverage]
class Markdown(
    IDocumentWalker<MarkdownWriterContext> dotNetXmlDocumentWalker,
    IDocumentVisitor<MarkdownWriterContext> markdownWriterVisitor)
    : IMarkdown
{
    public Task ConvertAsync(
        XDocument document,
        TextWriter markdownWriter,
        Predicate<DocumentTypeName> filter,
        CancellationToken cancellationToken)
        => dotNetXmlDocumentWalker.WalkAsync(
            new MarkdownWriterContext(filter, markdownWriter),
            document,
            markdownWriterVisitor,
            cancellationToken);
}