namespace Build.Core.Doc;

using Immutype;

[ExcludeFromCodeCoverage]
[Target]
public record Sample(
    string Description,
    IReadOnlyCollection<string>? Arguments = null);