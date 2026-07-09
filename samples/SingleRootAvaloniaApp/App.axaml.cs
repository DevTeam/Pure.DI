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
            var root = composition.Root;

            // Assigns the main window/view
            switch (ApplicationLifetime)
            {
                case IClassicDesktopStyleApplicationLifetime desktop:
                    desktop.MainWindow = root.CreateMainWindow();
                    break;

                case ISingleViewApplicationLifetime singleView:
                    singleView.MainView = root.CreateMainWindow();
                    break;
            }

            // Handles disposables
            if (ApplicationLifetime is IControlledApplicationLifetime controlledLifetime)
            {
                controlledLifetime.Exit += (_, _) => {
                    // Disposal of root objects with lifetime Transient, PerBlock, PerResolve
                    root.Owned.Dispose();
                    // Dispose of singletons
                    composition.Dispose();
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
