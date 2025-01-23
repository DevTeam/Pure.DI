using System;

public class ClockService : IClockService
{
    public DateTime Now => DateTime.Now;
}