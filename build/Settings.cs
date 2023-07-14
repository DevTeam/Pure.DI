using NuGet.Versioning;
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable CheckNamespace
// ReSharper disable NotAccessedPositionalProperty.Global

record Settings(
    bool BuildServer,
    string Configuration,
    VersionRange VersionRange,
    string NuGetKey,
    params BuildCase[] Cases);

record BuildCase(Version AnalyzerRoslynPackageVersion);