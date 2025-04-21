namespace MAUIReactorApp.Components;

using System.ComponentModel;

public class ClockState
{
    public int Counter { get; set; }
}

public class ClockPage : Component<ClockState>
{
    [Dependency]
    public IClockViewModel? ClockViewModel;

    public ClockPage()
    {
        Services.GetRequiredService<Composition>().BuildUp(this);
        if (ClockViewModel is INotifyPropertyChanged model)
        {
            model.PropertyChanged += (_, _) => Invalidate();
        }
    }

    public override VisualNode Render()
    {
        return ContentPage(
            ScrollView(
                VStack(
                        Image("dotnet_bot.png")
                            .HeightRequest(200)
                            .HCenter(),
                        Label($"{ClockViewModel!.Date} {ClockViewModel.Time}")
                            .FontSize(32)
                            .HCenter(),
                        Label("Welcome to MauiReactor Pure.DI example!")
                            .FontSize(18)
                            .HCenter(),
                        Button(State.Counter == 0 ? "Click me" : $"Clicked {State.Counter} times!")
                            .OnClicked(() => SetState(s => s.Counter += 1))
                            .HCenter()
                    )
                    .VCenter()
                    .Spacing(25)
                    .Padding(30, 0)
            )
        );
    }
}