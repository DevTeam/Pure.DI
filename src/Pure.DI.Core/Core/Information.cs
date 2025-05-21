namespace Pure.DI.Core;

using System.Reflection;

sealed class Information(Assembly assembly) : IInformation
{
    private readonly Lazy<string> _description = new(() => {
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return !string.IsNullOrWhiteSpace(version) ? $"{Names.GeneratorName} {version}" : Names.GeneratorName;
    });

    public string ShortDescription
    {
        get
        {
            var versionFinishIndex = Description.IndexOf("+", StringComparison.Ordinal);
            return versionFinishIndex < 1 ? Description : Description[..versionFinishIndex];
        }
    }

    public string Description => _description.Value;
}