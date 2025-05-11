namespace Clock.Tests;

public class ClockViewModelTest
{
    private readonly Mock<IClockModel> _clock = new();
    private readonly Mock<ITicks> _ticks = new();
    private readonly Mock<IDispatcher> _dispatcher = new();

    public ClockViewModelTest()
    {
        _dispatcher.Setup(i => i.Dispatch(It.IsAny<Action>())).Callback<Action>(action => action());
        _ticks.Raise(i => i.Tick += null);
    }

    [Fact]
    public void ShouldProvideDateTimeToDisplay()
    {
        // Given
        var now = new DateTimeOffset(2020, 8, 19, 19, 39, 47, TimeSpan.Zero);
        _clock.SetupGet(i => i.Now).Returns(now);
        var viewModel = CreateInstance();

        // When
        _ticks.Raise(i => i.Tick += null);
        var date = viewModel.Date;
        var time = viewModel.Time;

        // Then
        date.ShouldBe(now.ToString("d"));
        time.ShouldBe(now.ToString("T"));
    }

    [Fact]
    public void ShouldRefreshDateTimeWhenTimerTick()
    {
        // Given
        var viewModel = CreateInstance();

        var propertyNames = new List<string?>();
        viewModel.PropertyChanged += (_, args) => { propertyNames.Add(args.PropertyName); };

        // When
        var now1 = new DateTimeOffset(2020, 8, 19, 19, 39, 47, TimeSpan.Zero);
        _clock.SetupGet(i => i.Now).Returns(now1);
        _ticks.Raise(i => i.Tick += null);

        var now2 = new DateTimeOffset(2020, 8, 19, 19, 44, 48, TimeSpan.Zero);
        _clock.SetupGet(i => i.Now).Returns(now2);
        _ticks.Raise(i => i.Tick += null);

        // Then
        propertyNames.AsEnumerable().Count(i => i == nameof(IAppViewModel.Title)).ShouldBe(2);
        propertyNames.AsEnumerable().Count(i => i == nameof(IClockViewModel.Date)).ShouldBe(2);
        propertyNames.AsEnumerable().Count(i => i == nameof(IClockViewModel.Time)).ShouldBe(2);
    }

    private ClockViewModel CreateInstance() =>
        new(Mock.Of<ILog<ClockViewModel>>(), _clock.Object, _ticks.Object) { Dispatcher = _dispatcher.Object };
}