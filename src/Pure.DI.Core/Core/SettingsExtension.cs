namespace Pure.DI.Core;

internal static class SettingsExtension
{
    public static SettingState GetState(this IHints hints, Hint hint, SettingState defaultValue = SettingState.Off) =>
        hints.TryGetValue(hint, out var valueStr) && Enum.TryParse<SettingState>(valueStr, true, out var value)
            ? value
            : defaultValue;
    
    public static string GetValueOrDefault(this IHints hints, Hint hint, string defaultValue) =>
        hints.TryGetValue(hint, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : defaultValue;
}