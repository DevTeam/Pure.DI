using UnityEngine;

public class ClockViewModel : IClockViewModel
{
    const float HoursToDegrees = -30f, MinutesToDegrees = -6f, SecondsToDegrees = -6f;
    private readonly IClockService _clockService;

    public ClockViewModel(IClockService clockService)
    {
        _clockService = clockService;
    }

    public Quaternion HoursRotation { get; private set; }
    
    public Quaternion MinutesRotation { get; private set; }
    
    public Quaternion SecondsRotation { get; private set; }
    
    public void Update()
    {
        var now = _clockService.Now.TimeOfDay;
        HoursRotation = Quaternion.Euler(0f, 0f, HoursToDegrees * (float)now.TotalHours);
        MinutesRotation = Quaternion.Euler(0f, 0f, MinutesToDegrees * (float)now.TotalMinutes);
        SecondsRotation = Quaternion.Euler(0f, 0f, SecondsToDegrees *  (float)now.TotalSeconds);
    }
}