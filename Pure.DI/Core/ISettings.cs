namespace Pure.DI.Core;

internal interface ISettings
{
    bool Debug { get; }

    bool Trace { get; }

    bool TryGetOutputPath(out string outputPath);

    Verbosity Verbosity { get; }

    bool TryGetLogFile(out string logFilePath);

    Accessibility Accessibility { get; }

    SyntaxKind AccessibilityToken { get; }
}