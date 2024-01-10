using NuGet.Versioning;

namespace Build;

internal record Settings(
    bool BuildServer,
    string Configuration,
    VersionRange VersionRange,
    NuGetVersion? VersionOverride,
    string NuGetKey,
    params CodeAnalysis[] CodeAnalysis);