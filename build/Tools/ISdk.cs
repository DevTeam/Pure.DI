namespace Build.Tools;

using NuGet.Versioning;

internal interface ISdk
{
    IEnumerable<NuGetVersion> Versions { get; }
}