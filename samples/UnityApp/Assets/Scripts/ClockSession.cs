using System;
using System.Threading;
using UnityEngine;

public interface IClockSession
{
    int Id { get; }
}

public sealed class ClockSession : IClockSession, IDisposable
{
    private static int nextId;

    public int Id { get; } = Interlocked.Increment(ref nextId);

    public void Dispose()
    {
        Debug.Log($"Pure.DI scope {Id} is disposed.");
    }
}
