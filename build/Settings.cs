using NuGet.Versioning;
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable CheckNamespace
// ReSharper disable NotAccessedPositionalProperty.Global

record Settings(
    string Configuration,
    VersionRange VersionRange,
    string NuGetKey,
    params BuildCase[] Cases);

record BuildCase(Version AnalyzerRoslynPackageVersion);