namespace Build.Core;

sealed record ReadmeModel(
    string Header,
    string Introduction,
    string QuickStart,
    IReadOnlyList<ReadmeGroup> Groups,
    string Footer,
    string Contributing);

sealed record ReadmeGroup(string Title, string? Description, IReadOnlyList<ReadmeLink> Examples);

sealed record ReadmeLink(string Title, string File);

sealed record ReadmeCommand(string Aliases, string? Description);

sealed record ExamplePage(
    string Title,
    string Header,
    string Code,
    string Footer,
    string FrameworkVersion,
    IReadOnlyList<string> Packages,
    IReadOnlyList<string> GeneratedCode,
    string? ClassDiagram,
    IReadOnlyList<ReadmeLink> RelatedExamples);
