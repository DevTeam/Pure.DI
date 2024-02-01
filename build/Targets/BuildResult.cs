namespace Build.Targets;

using NuGet.Versioning;

internal record BuildResult(
    IReadOnlyCollection<string> Packages,
    NuGetVersion GeneratorPackageVersion,
    string GeneratorPackage,
    IReadOnlyCollection<Library> Libraries);