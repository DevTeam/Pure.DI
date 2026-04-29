using Pure.DI;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0649

public class ClockDigital : MonoBehaviour
{
    [SerializeField] private Text timeText;
    [SerializeField] private float labelSpacing = 320f;

    [Dependency]
    public IClockService ClockService { private get; set; }

    [Dependency]
    public IClockSession ClockSession { private get; set; }

    void Start()
    {
        timeText.rectTransform.anchoredPosition +=
            new Vector2((ClockSession.Id - 1.5f) * labelSpacing, 0f);

        Debug.Log(
            $"Digital clock '{name}' in scene '{gameObject.scene.name}' uses Pure.DI scope {ClockSession.Id}.");
    }

    void FixedUpdate()
    {
        var now = ClockService.Now;
        timeText.text =
            $"{now:HH:mm:ss}\nScope: {ClockSession.Id}";
    }
}
