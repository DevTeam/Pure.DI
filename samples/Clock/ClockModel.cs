namespace Clock;

public sealed class ClockModel: IClockModel
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}