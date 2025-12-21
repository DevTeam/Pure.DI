using Pure.DI;
using UnityEngine;
#pragma warning disable CS0649

public class ClockScope : MonoBehaviour
{
    [SerializeField] private ClockConfig _config;

    public IClockConfig Config => _config;

    void Start()
    {
        // Injects dependencies
        Composition.Shared.BuildUp(this);
    }
}