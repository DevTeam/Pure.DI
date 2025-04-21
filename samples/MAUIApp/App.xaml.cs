namespace MAUIApp;

public partial class App
{
    private readonly Composition _composition;

    internal App(Composition composition)
    {
        InitializeComponent();
        _composition = composition;
        Resources["Composition"] = composition;
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(_composition.AppShell);
}