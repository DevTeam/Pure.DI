namespace Pure.DI.Core;

using System.Reflection;

sealed class Information(Assembly assembly) : IInformation
{
    private readonly Lazy<string> _description = new(() => {
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return !string.IsNullOrWhiteSpace(version) ? $"{Names.GeneratorName} {version}" : Names.GeneratorName;
    });

    public string Description => _description.Value;
}