using NuGet.Versioning;
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable CheckNamespace
// ReSharper disable NotAccessedPositionalProperty.Global

record Settings(
    string Configuration,
    NuGetVersion DefaultVersion,
    string NuGetKey,
    params BuildCase[] Cases);

record BuildCase(Version AnalyzerRoslynPackageVersion);