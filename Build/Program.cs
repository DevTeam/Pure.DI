using CoreHtmlToImage;
using NuGet.Versioning;
using Microsoft.Extensions.DependencyInjection;

Tools.CheckRequiredSdk(new Version(6, 0, 301));

var version = NuGetVersion.Parse(Property.Get("version", "1.0.0-dev", true));
var nuGetKey = Property.Get("NuGetKey", string.Empty);
var requiredSdkRange = VersionRange.Parse(Property.Get("RequiredSdkRange", "[6.0, )"), false);
var configuration = Environment.OSVersion.Platform == PlatformID.Win32NT ? "Release" : "Linux";

var settings = new Settings(
    configuration,
    version,
    nuGetKey,
    requiredSdkRange,
    new BuildCase(new Version(3, 8, 0)),
    new BuildCase(new Version(4, 0, 1)));

GetService<IServiceCollection>()
    .AddSingleton<Root>()
    .AddSingleton(_ => settings)
    .AddSingleton<Build>()
    .AddSingleton<Deploy>()
    .AddSingleton<DeployTemplate>()
    .AddSingleton<Benchmark>()
    .AddSingleton<HtmlConverter>() 
.BuildServiceProvider()
.GetRequiredService<Root>()
.Run();