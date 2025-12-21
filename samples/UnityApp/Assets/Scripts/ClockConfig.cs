using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ClockConfig", menuName = "Clock/Config")]
public class ClockConfig : ScriptableObject, IClockConfig
{
    [SerializeField] private int _offsetSeconds;

    public TimeSpan Offset => TimeSpan.FromSeconds(_offsetSeconds);
}