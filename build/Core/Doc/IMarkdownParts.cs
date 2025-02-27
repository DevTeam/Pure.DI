namespace Build.Core.Doc;

interface IMarkdownParts
{
    Memory<string> GetParts(string? text);

    string Join(ReadOnlyMemory<string> parts);
}