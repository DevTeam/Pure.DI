using Pure.DI;
using UnityEngine;
using static Pure.DI.Lifetime;

internal partial class Composition
{
    public static readonly Composition Shared = new();
    
    private void Setup() => DI.Setup()
        .Bind().As(Singleton).To<ClockModel>()
        .Builders<MonoBehaviour>();
}
