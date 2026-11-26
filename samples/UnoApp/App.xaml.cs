// ReSharper disable RedundantExtendsListEntry
// The XAML part already specifies the base type, but the Uno XAML compiler requires it in the code-behind too (UXAML0002).
namespace UnoApp;

using System.ComponentModel;

public partial class App : Application
{
    public App() => InitializeComponent();

    // Keeps the main window reachable after OnLaunched returns; the reference is released on close.
    private Window? MainWindow { get; set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (Resources[nameof(Composition)] is not Composition composition)
        {
            throw new InvalidOperationException("The Pure.DI composition resource was not found.");
        }

        var window = MainWindow = new Window
        {
            Content = new MainPage()
        };

        var appViewModel = composition.App;
        window.Title = appViewModel.Title;

        PropertyChangedEventHandler? titleChanged = null;
        if (appViewModel is INotifyPropertyChanged propertyChanged)
        {
            titleChanged = (_, eventArgs) =>
            {
                if (eventArgs.PropertyName is null or nameof(IAppViewModel.Title))
                {
                    window.Title = appViewModel.Title;
                }
            };

            propertyChanged.PropertyChanged += titleChanged;
        }

        // Releases disposable singleton and scoped dependencies owned by Pure.DI.
        window.Closed += (_, _) =>
        {
            if (appViewModel is INotifyPropertyChanged closedPropertyChanged && titleChanged is not null)
            {
                closedPropertyChanged.PropertyChanged -= titleChanged;
            }

            if (ReferenceEquals(MainWindow, window))
            {
                MainWindow = null;
            }

            composition.Dispose();
        };

        window.Activate();
    }
}
