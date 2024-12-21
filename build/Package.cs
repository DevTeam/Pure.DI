namespace Build;

using NuGet.Versioning;

internal record Package(
    string Path,
    bool Deploy,
    NuGetVersion Version);