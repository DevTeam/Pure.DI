#pragma warning disable CS8602 // Dereference of a possibly null reference.
namespace MAUIReactorApp;

public class ClockState;

public class ClockPage : Component<ClockState>
{
    [Dependency]
    public IAppViewModel? App;

    [Dependency]
    public IClockViewModel? Clock;

    public ClockPage()
    {
        Services.GetRequiredService<Composition>().BuildUp(this);
        if (App is INotifyPropertyChanged appModel)
        {
            appModel.PropertyChanged += (_, _) => Invalidate();
        }

        if (Clock is INotifyPropertyChanged clockModel)
        {
            clockModel.PropertyChanged += (_, _) => Invalidate();
        }
    }

    public override VisualNode Render() =>
        ContentPage(
            ScrollView(
                VStack(
                        Image("dotnet_bot.png")
                            .HeightRequest(200)
                            .HCenter(),
                        Label($"{Clock.Date} {Clock.Time}")
                            .FontSize(32)
                            .HCenter()
                    )
                    .VCenter()
                    .Spacing(25)
                    .Padding(30, 0)
            )
        ).Title(App.Title);
}