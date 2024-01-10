using System.CommandLine;
using Build;using HostApi;
using NuGet.Versioning;

Directory.SetCurrentDirectory(Tools.GetSolutionDirectory());
await new DotNetBuildServerShutdown().RunAsync();

WriteLine(
    NuGetVersion.TryParse("version".Get(), out var version) 
        ? $"The version has been overridden by {version}."
        : "The next version has been used.",
    Color.Highlighted);

var settings = new Settings(
    Environment.GetEnvironmentVariable("TEAMCITY_VERSION") is not null,
    "Release",
    VersionRange.Parse("2.0.*"),
    version,
    "NuGetKey".Get(),
    new CodeAnalysis(new Version(4, 3, 1)));

var composition = new Composition(settings);
return await composition.Root.InvokeAsync(Args.ToArray());