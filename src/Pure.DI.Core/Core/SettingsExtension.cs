namespace Pure.DI.Core;

internal static class SettingsExtension
{
    public static SettingState GetState(this ISettings settings, Setting setting, SettingState defaultValue = SettingState.Off) =>
        settings.TryGetValue(setting, out var valueStr) && Enum.TryParse<SettingState>(valueStr, out var value)
            ? value
            : defaultValue;
}