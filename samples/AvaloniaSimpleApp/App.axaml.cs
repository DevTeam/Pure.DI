using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace AvaloniaSimpleApp;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && Resources[nameof(Composition)] is Composition composition)
        {
            desktop.MainWindow = composition.MainWindow;
        }
        
        base.OnFrameworkInitializationCompleted();
    }
}