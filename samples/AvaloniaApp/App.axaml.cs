using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace AvaloniaApp;

using Views;

public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Assignment of the main window
            desktop.MainWindow = new MainWindow();
            // Handles disposables
            desktop.Exit += (_, _) => ((Composition)Resources["Composition"]!).Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}