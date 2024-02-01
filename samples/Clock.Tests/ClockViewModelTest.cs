namespace Clock.Tests;

using Models;

public class ClockViewModelTest
{
    private readonly Mock<IDispatcher> _dispatcher = new();

    public ClockViewModelTest() =>
        _dispatcher.Setup(i => i.Dispatch(It.IsAny<Action>()))
            .Callback<Action>(action => action());

    [Fact]
    public void ShouldProvideDateTimeToDisplay()
    {
        // Given
        var now = new DateTimeOffset(2020, 8, 19, 19, 39, 47, TimeSpan.Zero);
        var clock = new Mock<IClock>();
        clock.SetupGet(i => i.Now).Returns(now);

        var viewModel = new ClockViewModel(
            Mock.Of<ILog<ClockViewModel>>(),
            clock.Object,
            Mock.Of<ITimer>())
        {
            Dispatcher = _dispatcher.Object,
            Log = Mock.Of<ILog<ViewModel>>()
        };

        // When
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
        var timer = new Mock<ITimer>();
        IObserver<Tick>? observer = null;
        timer
            .Setup(i => i.Subscribe(It.IsAny<IObserver<Tick>>()))
            .Callback(new Action<IObserver<Tick>>(o => { observer = o; }))
            .Returns(Mock.Of<IDisposable>());

        var viewModel = new ClockViewModel(
            Mock.Of<ILog<ClockViewModel>>(),
            Mock.Of<IClock>(),
            timer.Object)
        {
            Dispatcher = _dispatcher.Object,
            Log = Mock.Of<ILog<ViewModel>>()
        };

        var propertyNames = new List<string?>();
        viewModel.PropertyChanged += (_, args) => { propertyNames.Add(args.PropertyName); };

        // When
        observer?.OnNext(Tick.Shared);
        observer?.OnNext(Tick.Shared);

        // Then
        propertyNames.Count(i => i == nameof(IClockViewModel.Date)).ShouldBe(2);
        propertyNames.Count(i => i == nameof(IClockViewModel.Time)).ShouldBe(2);
    }

    [Fact]
    public void ShouldDisposeTimerSubscriptionWhenDispose()
    {
        // Given
        var subscription = new Mock<IDisposable>();
        var timer = new Mock<ITimer>();
        timer
            .Setup(i => i.Subscribe(It.IsAny<IObserver<Tick>>()))
            .Returns(subscription.Object);

        var viewModel = new ClockViewModel(
            Mock.Of<ILog<ClockViewModel>>(),
            Mock.Of<IClock>(),
            timer.Object)
        {
            Dispatcher = _dispatcher.Object,
            Log = Mock.Of<ILog<ViewModel>>()
        };

        // When
        ((IDisposable)viewModel).Dispose();

        // Then
        subscription.Verify(i => i.Dispose(), Times.Once);
    }
}