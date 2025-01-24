using System;
using Pure.DI;
using UnityEngine;
#pragma warning disable CS8618
#pragma warning disable CS0649

public class Clock : MonoBehaviour
{
    [SerializeField]
    private Transform hoursPivot;
    
    [SerializeField]
    private Transform minutesPivot;

    [SerializeField]
    private Transform secondsPivot;

    [Dependency]
    public IClockViewModel ClockViewModel { private get; set; }

    void Start()
    {
        // Injects dependencies
        Composition.Shared.BuildUp(this);
    }

    void Update()
    {
        ClockViewModel.Update();
        hoursPivot.localRotation = ClockViewModel.HoursRotation;
        minutesPivot.localRotation = ClockViewModel.MinutesRotation;
        secondsPivot.localRotation = ClockViewModel.SecondsRotation;
    }
}
