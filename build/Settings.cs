using NuGet.Versioning;
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable CheckNamespace

record Settings(
    string Configuration,
    NuGetVersion DefaultVersion,
    string NuGetKey,
    VersionRange RequiredSdkRange,
    params BuildCase[] Cases);

record BuildCase(Version AnalyzerRoslynPackageVersion);