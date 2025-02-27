namespace Build.Core.Doc;

record MarkdownWriterContext(
    Predicate<DocumentTypeName> Filter,
    TextWriter Writer,
    string Namespace = "",
    bool IsSkipping = false,
    bool TrimStart = false);