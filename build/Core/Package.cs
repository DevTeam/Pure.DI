namespace Build.Core;

using NuGet.Versioning;

record Package(
    string Path,
    bool Deploy,
    NuGetVersion Version);