using System;

public interface IClockConfig
{
    TimeSpan Offset { get; }
}