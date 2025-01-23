using UnityEngine;

public interface IClockViewModel
{
    Quaternion HoursRotation { get; }

    Quaternion MinutesRotation { get; }

    Quaternion SecondsRotation { get; }

    void Update();
}