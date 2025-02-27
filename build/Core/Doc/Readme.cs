namespace Build.Core.Doc;

using Immutype;

[ExcludeFromCodeCoverage]
[Target]
public record Readme(
    string Header = "",
    string Usage = "",
    string Sample = "",
    IReadOnlyCollection<string>? CommonArguments = null);