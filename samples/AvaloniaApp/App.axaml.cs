namespace AvaloniaApp;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (Resources[nameof(Composition)] is Composition composition)
        {
            // Assigns the main window/view
            switch (ApplicationLifetime)
            {
                case IClassicDesktopStyleApplicationLifetime desktop:
                    desktop.MainWindow = composition.MainWindow;
                    break;

                case ISingleViewApplicationLifetime singleView:
                    singleView.MainView = composition.MainWindow;
                    break;
            }

            // Handles disposables
            if (ApplicationLifetime is IControlledApplicationLifetime controlledLifetime)
            {
                controlledLifetime.Exit += (_, _) => composition.Dispose();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}