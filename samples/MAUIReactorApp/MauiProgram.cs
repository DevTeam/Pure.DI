namespace MAUIReactorApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        using var composition = new Composition();
        // Uses Composition as an alternative IServiceProviderFactory
        builder.ConfigureContainer(composition);

        builder
            .UseMauiApp<App>()
            // or .UseMauiApp(serviceProvider => new App(serviceProvider))
            .ConfigureFonts(fonts => {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        return builder.Build();
    }
}