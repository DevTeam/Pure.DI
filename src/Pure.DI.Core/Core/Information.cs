namespace Pure.DI.Core;

using System.Reflection;

sealed class Information(Assembly assembly) : IInformation
{
    private readonly Lazy<string> _version = new(() => GetVersion(assembly));

    private readonly Lazy<string> _description = new(() => GetDescription(assembly));

    public string Name => Names.GeneratorName;

    public string Version => _version.Value;

    public string ShortDescription
    {
        get
        {
            var versionFinishIndex = Description.IndexOf("+", StringComparison.Ordinal);
            return versionFinishIndex < 1 ? Description : Description[..versionFinishIndex];
        }
    }

    public string Description => _description.Value;

    private static string GetDescription(Assembly assembly)
    {
        var version = GetVersion(assembly);
        return !string.IsNullOrWhiteSpace(version) ? $"{Names.GeneratorName} {version}" : Names.GeneratorName;
    }

    private static string GetVersion(Assembly assembly) =>
        assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
}
