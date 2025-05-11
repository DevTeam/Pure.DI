namespace Clock;

public interface IClockModel
{
    DateTimeOffset Now { get; }
}