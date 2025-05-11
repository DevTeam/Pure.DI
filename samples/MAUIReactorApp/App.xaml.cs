using MauiReactor;
using MAUIReactorApp.Resources.Styles;

namespace MAUIReactorApp;

public partial class App
{
    public App(IServiceProvider serviceProvider)
        : base(serviceProvider) =>
        InitializeComponent();
}

public abstract class MauiReactorApplication : ReactorApplication<ClockPage>
{
    protected MauiReactorApplication(IServiceProvider serviceProvider)
        : base(serviceProvider) =>
        this.UseTheme<ApplicationTheme>();
}