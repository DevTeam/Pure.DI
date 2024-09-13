namespace Pure.DI.Core;

using System.Reflection;

internal sealed class Information : IInformation
{
    private static readonly string CurrentDescription = Names.GeneratorName;

    static Information()
    {
        var assembly = typeof(Information).Assembly;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(version))
        {
            CurrentDescription = $"{Names.GeneratorName} {version}";
        }
    }

    public string Description => CurrentDescription;
}