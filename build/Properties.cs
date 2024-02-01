// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable ClassNeverInstantiated.Global
namespace Build;

using System.Diagnostics.CodeAnalysis;
using HostApi;

internal class Properties
{
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public string Get(string name, string defaultProp = "", bool showWarning = false) =>
        Get(Props, name, defaultProp, showWarning);

    private static string Get(IProperties props, string name, string defaultProp, bool showWarning = false)
    {
        if (props.TryGetValue(name, out var prop) && !string.IsNullOrWhiteSpace(prop))
        {
            return prop;
        }

        var message = $"The property \"{name}\" was not defined, the default value \"{defaultProp}\" was used.";
        if (showWarning)
        {
            Warning(message);
        }
        else
        {
            WriteLine(message, Color.Details);
        }

        return defaultProp;
    }
}