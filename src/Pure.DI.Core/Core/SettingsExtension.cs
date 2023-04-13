namespace Pure.DI.Core;

internal static class SettingsExtension
{
    public static SettingState GetState(this ISettings settings, Setting setting, SettingState defaultValue = SettingState.Off) =>
        settings.TryGetValue(setting, out var valueStr) && Enum.TryParse<SettingState>(valueStr, true, out var value)
            ? value
            : defaultValue;
    
    public static string GetValueOrDefault(this ISettings settings, Setting setting, string defaultValue) =>
        settings.TryGetValue(setting, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : defaultValue;
}