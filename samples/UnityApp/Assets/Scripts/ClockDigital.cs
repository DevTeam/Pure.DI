using Pure.DI;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0649

public class ClockDigital : MonoBehaviour
{
    [SerializeField]
    private Text timeText;

    [Dependency]
    public IClockService ClockService { private get; set; } // How to resolve this?

    void FixedUpdate()
    {
        if (timeText == null || ClockService == null)
        {
            return;
        }

        var now = ClockService.Now;

        timeText.text = now.ToString("HH:mm:ss");
    }
}
