namespace Build.Core.Doc;

using System.Xml.Linq;

[ExcludeFromCodeCoverage]
class XDocumentTools
{
    public Task<XDocument> LoadAsync(TextReader textReader, LoadOptions options, CancellationToken cancellationToken) =>
        XDocument.LoadAsync(textReader, options, cancellationToken);
}