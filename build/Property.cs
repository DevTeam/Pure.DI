namespace Build;

using HostApi;

internal static class Property
{
    public static string Get(this string name, string defaultProp = "", bool showWarning = false) =>
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
            Info(message);
        }

        return defaultProp;
    }
}