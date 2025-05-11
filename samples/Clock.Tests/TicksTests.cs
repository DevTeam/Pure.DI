namespace Clock.Tests;

public sealed class TicksTests: IDisposable
{
    private readonly Timer _timer = new(_ => { }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

    [Fact]
    public void ShouldRaiseTickEvent()
    {
        var ticksCount = 0;

        // Given
        var ticks = CreateInstance();
        ticks.Tick += () => ticksCount++;

        // When
        ticks.OnTick();
        ticks.OnTick();
        ticks.OnTick();

        // Then
        ticksCount.ShouldBe(3);
    }

    [Fact]
    public void ShouldRaiseTickEventForMultipleSubscribers()
    {
        var ticksCount = 0;

        // Given
        var ticks = CreateInstance();
        ticks.Tick += () => ticksCount++;
        ticks.Tick += () => ticksCount++;

        // When
        ticks.OnTick();
        ticks.OnTick();

        // Then
        ticksCount.ShouldBe(4);
    }

    [Fact]
    public void ShouldSupportScenarioWithoutAnySubscribers()
    {
        // Given
        var ticks = CreateInstance();

        // When
        ticks.OnTick();
        ticks.OnTick();

        // Then
    }

    public void Dispose() => _timer.Dispose();

    private Ticks CreateInstance() =>
        new(Mock.Of<ILog<Ticks>>(), TimeSpan.Zero, _timer);
}