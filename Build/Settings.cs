using NuGet.Versioning;

record Settings(
    string configuration,
    NuGetVersion DefaultVersion,
    string NuGetKey,
    VersionRange RequiredSdkRange,
    params BuildCase[] Cases);

record BuildCase(Version AnalyzerRoslynPackageVersion);