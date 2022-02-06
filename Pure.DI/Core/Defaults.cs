namespace Pure.DI.Core;

internal static class Defaults
{
    private const string TempNamespace = "NS35EBD81B";
    // ReSharper disable once InconsistentNaming
    private const string _defaultNamespace = "Pure.DI";

    [ThreadStatic] private static string? _currentDefaultNamespace;

    public static string DefaultNamespace
    {
        get => string.IsNullOrWhiteSpace(_currentDefaultNamespace) ? _defaultNamespace : _currentDefaultNamespace!;
        set => _currentDefaultNamespace = value;
    }

    public static string ReplaceNamespace(this string code) =>
        code.Replace(TempNamespace, DefaultNamespace);
}