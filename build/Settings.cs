using NuGet.Versioning;
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable CheckNamespace
// ReSharper disable NotAccessedPositionalProperty.Global

record Settings(
    bool BuildServer,
    string Configuration,
    VersionRange VersionRange,
    NuGetVersion? VersionOverride,
    string NuGetKey,
    params CodeAnalysis[] CodeAnalysis);

record CodeAnalysis(Version AnalyzerRoslynPackageVersion);