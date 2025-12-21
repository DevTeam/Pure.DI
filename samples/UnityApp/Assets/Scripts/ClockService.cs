using System;

public class ClockService : IClockService, IDisposable
{
    private readonly IClockConfig _config;

    public DateTime Now => DateTime.UtcNow + _config.Offset;

    public ClockService(IClockConfig config)
    {
        _config = config;
    }

    public void Dispose()
    {
        // Perform any necessary cleanup here
    }
}