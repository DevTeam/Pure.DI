namespace MAUIApp;

public partial class App: Application
{
    internal App(Composition composition)
    {
        InitializeComponent();
        Resources["Composition"] = composition;
        MainPage = composition.AppShell;
    }
}