namespace Build.Core.Doc;

using System.Xml.Linq;
using System.Xml.XPath;

class DotNetXmlDocumentWalker<T>(MarkdownParts markdownParts) : IDocumentWalker<T>
{
    public async Task WalkAsync(
        T ctx,
        XDocument document,
        IDocumentVisitor<T> visitor,
        CancellationToken cancellationToken)
    {
        var items = (
                from element in document.XPathSelectElements("/doc/members/member")
                let name = element.Attribute("name")?.Value
                where !string.IsNullOrWhiteSpace(name)
                where name.Length >= 3
                let kind = char.ToUpper(name[0])
                let parts = markdownParts.GetParts(name[2..])
                where parts.Length > 1
                let typeFullNameParts = kind == 'T' ? parts : parts[..^1]
                let namespaceName = markdownParts.Join(typeFullNameParts[..^1])
                let typeName = markdownParts.Join(typeFullNameParts[^1..]) ?? ""
                let memberName = markdownParts.Join(parts[^1..]) ?? ""
                select (name: new DocumentTypeName(namespaceName, typeName, memberName), kind, element))
            .OrderBy(i => i.name.NamespaceName).ThenBy(i => i.name.TypeName);

        DocumentType? type = null;
        foreach (var (name, kind, element) in items)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var memberName = name.MemberName;
            DocumentMember? member = null;
            switch (kind)
            {
                case 'T':
                    if (type is {} curType)
                    {
                        ctx = await visitor.FinishTypeVisitAsync(ctx, curType, cancellationToken);
                    }

                    type = new DocumentType(name);
                    ctx = await visitor.StartTypeVisitAsync(ctx, type, cancellationToken);
                    ctx = await ExplorerContainerAsync(ctx, element, visitor, cancellationToken);
                    break;

                case 'F':
                    member = new DocumentMember(name, DocumentMemberKind.Field);
                    break;

                case 'P':
                    member = new DocumentMember(name, DocumentMemberKind.Property);
                    break;

                case 'M':
                    if (memberName.StartsWith("#ctor"))
                    {
                        member =
                            new DocumentMember(
                                name with { MemberName = name.MemberName.Replace("#ctor", name.TypeName) },
                                DocumentMemberKind.Constructor);
                    }
                    else
                    {
                        if (memberName.StartsWith("op_Implicit"))
                        {
                            member =
                                new DocumentMember(
                                    name with { MemberName = name.MemberName.Replace("op_Implicit", "") },
                                    DocumentMemberKind.ImplicitOperator);
                        }
                        else
                        {
                            member = new DocumentMember(name, DocumentMemberKind.Method);
                        }
                    }

                    break;
            }

            if (member is not {} curMember)
            {
                continue;
            }

            ctx = await visitor.StartMemberVisitAsync(ctx, curMember, cancellationToken);
            ctx = await ExplorerContainerAsync(ctx, element, visitor, cancellationToken);
            ctx = await visitor.FinishMemberVisitAsync(ctx, curMember, cancellationToken);
        }

        if (type is {} lastType)
        {
            ctx = await visitor.FinishTypeVisitAsync(ctx, lastType, cancellationToken);
        }

        await visitor.CompletedAsync(ctx, cancellationToken);
    }

    private static async Task<T> ExplorerContainerAsync(T ctx, XContainer container, IDocumentVisitor<T> visitor, CancellationToken cancellationToken)
    {
        foreach (var node in container.Nodes())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            switch (node)
            {
                case XElement element:
                    var markdownElement = new DocumentElement(element);
                    ctx = await visitor.StartElementVisitAsync(ctx, markdownElement, cancellationToken);
                    ctx = await ExplorerContainerAsync(ctx, element, visitor, cancellationToken);
                    ctx = await visitor.FinishElementVisitAsync(ctx, markdownElement, cancellationToken);

                    break;

                case XText text:
                    ctx = await visitor.TextVisitAsync(ctx, new DocumentText(text), cancellationToken);
                    break;
            }
        }

        return ctx;
    }
}