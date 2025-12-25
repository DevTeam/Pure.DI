using System;
using UnityEngine;

public class ClockManager : IDisposable
{
    private readonly Scope _scope;
    private readonly IClockConfig _config;

    public ClockManager(Scope scope, IClockConfig config)
    {
        _scope = scope;
        _config = config;
    }

    public void Start()
    {
        // Important: Start it just sample. Game can spawn any object at any time
        if (_config.ShowDigital)
        {
            _scope.BuildUp(GameObject.Instantiate(_config.ClockDigitalPrefab));
        }
    }

    public void Dispose()
    {
        // Perform any necessary cleanup here
    }
}