namespace Clock;

public sealed class DesignTimeClockViewModel: IClockViewModel
{
    private static readonly DateTimeOffset Now = new(2025, 11, 16, 17, 30, 41, TimeSpan.FromHours(3));

    public string Time => Now.ToString("T");

    public string Date => Now.ToString("d");
}