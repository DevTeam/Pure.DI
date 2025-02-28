namespace Build.Core.Doc;

record MarkdownWriterContext(
    Predicate<DocumentPart> DocumentPartFilter,
    TextWriter Writer,
    string Namespace = "",
    bool IsSkipping = false,
    bool IsSkippingMember = false,
    bool TrimStart = false);