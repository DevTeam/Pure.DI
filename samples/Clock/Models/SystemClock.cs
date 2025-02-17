namespace Clock.Models;

// ReSharper disable once ClassNeverInstantiated.Global
class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}