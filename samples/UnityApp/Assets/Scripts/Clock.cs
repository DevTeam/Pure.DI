using Pure.DI;
using UnityEngine;

public class Clock : MonoBehaviour
{
    const float HoursToDegrees = -30f, MinutesToDegrees = -6f, SecondsToDegrees = -6f;
    [SerializeField] Scope scope;
    [SerializeField] Transform hoursPivot;
    [SerializeField] Transform minutesPivot;
    [SerializeField] Transform secondsPivot;

    [Dependency]
    public IClockService ClockService { private get; set; }

    [Dependency]
    public IClockSession ClockSession { private get; set; }

    void Awake()
    {
        scope.EnsureReady();
        scope.BuildUp(this);
        Debug.Log(
            $"Clock '{name}' in scene '{gameObject.scene.name}' uses Pure.DI scope {ClockSession.Id}.");
    }

    void Update()
    {
        var now = ClockService.Now.TimeOfDay;
        hoursPivot.localRotation = Quaternion.Euler(0f, 0f, HoursToDegrees * (float)now.TotalHours);
        minutesPivot.localRotation = Quaternion.Euler(0f, 0f, MinutesToDegrees * (float)now.TotalMinutes);
        secondsPivot.localRotation = Quaternion.Euler(0f, 0f, SecondsToDegrees * (float)now.TotalSeconds);
    }
}
