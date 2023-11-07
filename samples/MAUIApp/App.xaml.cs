namespace MAUIApp;

public partial class App: Application
{
    internal App(
        Composition composition,
        Func<AppShell> appShellFactory)
    {
        InitializeComponent();
        
        Resources["Composition"] = composition;
        MainPage = appShellFactory();
    }
}