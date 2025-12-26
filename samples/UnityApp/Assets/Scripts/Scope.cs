using Pure.DI;
using UnityEngine;
using static Pure.DI.Lifetime;

public partial class Scope : MonoBehaviour
{
    [SerializeField] public ClockConfig clockConfig;

    void Setup() => DI.Setup()
        .Bind().To(_ => clockConfig)
        .Bind().As(Singleton).To<ClockService>()
        .Root<ClockManager>(nameof(ClockManager))
        .Builders<MonoBehaviour>();

    void Start()
    {
        ClockManager.Start();
    }

    void OnDestroy()
    {
        Dispose();
    }
}
