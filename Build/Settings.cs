using NuGet.Versioning;

record Settings(
    NuGetVersion DefaultVersion,
    string NuGetKey,
    VersionRange RequiredSdkRange,
    params BuildCase[] Cases);

record BuildCase(Version AnalyzerRoslynPackageVersion);