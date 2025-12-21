using System;

public class ClockService : IClockService
{
    private readonly IClockConfig _config;

    public DateTime Now => DateTime.Now + _config.Offset;

    public ClockService(IClockConfig config)
    {
        _config = config;
    }
}