namespace Pure.DI.Core;

internal static class SettingsExtension
{
    public static T GetHint<T>(this IHints hints, Hint hint, T defaultValue = default) 
        where T : struct =>
        hints.TryGetValue(hint, out var valueStr) && Enum.TryParse<T>(valueStr, true, out var value)
            ? value
            : defaultValue;
    
    public static string GetValueOrDefault(this IHints hints, Hint hint, string defaultValue) =>
        hints.TryGetValue(hint, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : defaultValue;
}