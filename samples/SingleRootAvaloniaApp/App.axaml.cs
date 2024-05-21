using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace AvaloniaApp;

public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && Resources["Composition"] is Composition composition)
        {
            // Assignment of the main window
            desktop.MainWindow = composition.App.MainWindow;
            // Handles disposables
            desktop.Exit += (_, _) => composition.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}