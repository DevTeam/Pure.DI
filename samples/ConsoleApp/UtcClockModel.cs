namespace ConsoleApp;

class UtcClockModel: IClockModel
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}