namespace Build.Core.Doc;

using System.Xml.Linq;

interface IDocumentWalker<T>
{
    Task WalkAsync(
        T ctx,
        XDocument document,
        IDocumentVisitor<T> visitor,
        CancellationToken cancellationToken);
}