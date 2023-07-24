namespace Pure.DI.Core;

using System.Reflection;

internal class Information : IInformation
{
    private static readonly string CurrentDescription = Constant.GeneratorName;
    
    static Information()
    {
        var assembly = typeof(Information).Assembly;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(version))
        {
            CurrentDescription = $"{Constant.GeneratorName} {version}";
        }
    }

    public string Description => CurrentDescription;
}