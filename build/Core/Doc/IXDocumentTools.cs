namespace Build.Core.Doc;

using System.Xml.Linq;

interface IXDocumentTools
{
    Task<XDocument> LoadAsync(TextReader textReader, LoadOptions options, CancellationToken cancellationToken);
}