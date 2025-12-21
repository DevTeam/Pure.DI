using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ClockConfig", menuName = "Clock/Config")]
public class ClockConfig : ScriptableObject, IClockConfig
{
    [SerializeField] int offsetHours;
    [SerializeField] bool showDigital;
    [SerializeField] ClockDigital clockDigitalPrefab;

    public TimeSpan Offset => TimeSpan.FromHours(offsetHours);
    public bool ShowDigital => showDigital;
    public ClockDigital ClockDigitalPrefab => clockDigitalPrefab;
}