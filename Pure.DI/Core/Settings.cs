// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable IdentifierTypo
namespace Pure.DI.Core;

using System.IO;

internal sealed class Settings : ISettings
{
    private readonly IBuildContext _buildContext;
    private readonly IFileSystem _fileSystem;
    private readonly IAccessibilityToSyntaxKindConverter _accessibilityToSyntaxKindConverter;

    public Settings(
        IBuildContext buildContext,
        IFileSystem fileSystem,
        IAccessibilityToSyntaxKindConverter accessibilityToSyntaxKindConverter)
    {
        _buildContext = buildContext;
        _fileSystem = fileSystem;
        _accessibilityToSyntaxKindConverter = accessibilityToSyntaxKindConverter;
    }

    public bool Debug => GetBool(Setting.Debug);

    public bool Trace => GetBool(Setting.Trace);

    public bool TryGetOutputPath(out string outputPath)
    {
        if (!TryGet(Setting.Out, out outputPath))
        {
            outputPath = string.Empty;
            return false;
        }

        outputPath = EnsureExists(Path.Combine(outputPath, _buildContext.Metadata.ComposerTypeName));
        return true;
    }

    public Verbosity Verbosity =>
        TryGet(Setting.Verbosity, out var verbosityValue)
        && Enum.TryParse(verbosityValue, true, out Verbosity verbosity)
            ? verbosity
            : Verbosity.Quiet;

    public bool TryGetLogFile(out string logFilePath)
    {
        if (Verbosity > Verbosity.Quiet)
        {
            logFilePath = GetFullPath($"{_buildContext.Metadata.ComposerTypeName}.log");
            return true;
        }

        logFilePath = string.Empty;
        return false;
    }

    public Accessibility Accessibility =>
        TryGet(Setting.Accessibility, out var accessibilityValue) && Enum.TryParse(accessibilityValue, true, out Accessibility accessibility)
            ? accessibility
            : Accessibility.Public;

    public SyntaxKind AccessibilityToken => _accessibilityToSyntaxKindConverter.Convert(Accessibility);

    public bool MEDI => GetBool(Setting.MEDI, true);

    public bool Nullability => (_buildContext.Compilation.Options.NullableContextOptions & NullableContextOptions.Enable) == NullableContextOptions.Enable;

    private bool TryGet(Setting setting, out string value)
    {
        var settings = _buildContext.Metadata.Settings;
        return settings.TryGetValue(setting, out value);
    }

    private bool GetBool(Setting setting, bool defaultValue = false) =>
        TryGet(setting, out var valueStr) && bool.TryParse(valueStr, out var value)
            ? value
            : defaultValue;

    private string GetFullPath(string path)
    {
        if (TryGetOutputPath(out var outputPath))
        {
            path = Path.Combine(outputPath, path);
        }

        return path;
    }

    private string EnsureExists(string path)
    {
        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), path);
        }

        _fileSystem.CreateDirectory(path);
        return path;
    }
}