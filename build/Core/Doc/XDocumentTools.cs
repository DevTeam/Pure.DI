// ReSharper disable MemberCanBeMadeStatic.Global
namespace Build.Core.Doc;

using System.Xml.Linq;

class XDocumentTools
{
    public Task<XDocument> LoadAsync(TextReader textReader, LoadOptions options, CancellationToken cancellationToken) =>
        XDocument.LoadAsync(textReader, options, cancellationToken);
}