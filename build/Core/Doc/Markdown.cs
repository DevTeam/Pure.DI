namespace Build.Core.Doc;

using System.Xml.Linq;

[ExcludeFromCodeCoverage]
class Markdown(
    IDocumentWalker<MarkdownWriterContext> dotNetXmlDocumentWalker,
    IDocumentVisitor<MarkdownWriterContext> markdownWriterVisitor)
{
    public Task ConvertAsync(
        XDocument document,
        TextWriter markdownWriter,
        Predicate<DocumentPart> documentPartFilter,
        CancellationToken cancellationToken)
        => dotNetXmlDocumentWalker.WalkAsync(
            new MarkdownWriterContext(documentPartFilter, markdownWriter),
            document,
            markdownWriterVisitor,
            cancellationToken);
}