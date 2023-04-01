namespace Pure.DI.Core;

internal static class SettingsExtension
{
    public static bool GetBool(this ISettings settings, Setting setting, bool defaultValue = false) =>
        settings.TryGetValue(setting, out var valueStr) && bool.TryParse(valueStr, out var value)
            ? value
            : defaultValue;
}