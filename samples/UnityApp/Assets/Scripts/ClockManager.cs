using System;
using UnityEngine;

public class ClockManager : IDisposable
{
    private readonly IClockConfig _config;

    public ClockManager(IClockConfig config)
    {
        _config = config;
    }

    public void Start()
    {
        // Important: Start it just sample. Game can spawn any object at any time
        if (_config.ShowDigital)
        {
            GameObject.Instantiate(_config.ClockDigitalPrefab);
        }
    }

    public void Dispose()
    {
        // Perform any necessary cleanup here
    }
}