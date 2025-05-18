using static Pure.DI.Tag;

using var composition = new Composition(TimeSpan.FromMilliseconds(100));
var root = composition.Root;

root.Run();

partial class Program(
    IClockModel clock,
    [Tag(Utc)] IClockModel utcClock,
    ITicks ticks,
    IConsole console)
{
    private void Run()
    {
        ticks.Tick += OnTick;
        console.WaitForKey();
        ticks.Tick -= OnTick;
    }

    private void OnTick()
    {
        var now = clock.Now;
        console.Write($"{now:d} {now:T}.{now:ff} (UTC: {utcClock.Now:g})");
    }
}