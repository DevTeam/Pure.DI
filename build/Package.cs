namespace Build;

using NuGet.Versioning;

record Package(
    string Path,
    bool Deploy,
    NuGetVersion Version);