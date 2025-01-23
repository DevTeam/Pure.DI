using System;
using Pure.DI;
using UnityEngine;

public class Clock : MonoBehaviour
{
    [SerializeField]
    private Transform hoursPivot;
    
    [SerializeField]
    private Transform minutesPivot;

    [SerializeField]
    private Transform secondsPivot;

    private IClockViewModel clockViewModel;

    [Ordinal(0)]
    public void Inject(IClockViewModel clockViewModel)
    {
        this.clockViewModel = clockViewModel;
    }

    void Start()
    {
        Composition.Shared.BuildUp(this);
    }

    void Update()
    {
        clockViewModel.Update();
        hoursPivot.localRotation = clockViewModel.HoursRotation;
        minutesPivot.localRotation = clockViewModel.MinutesRotation;
        secondsPivot.localRotation = clockViewModel.SecondsRotation;
    }
}
