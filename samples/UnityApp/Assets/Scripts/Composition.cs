using Pure.DI;
using UnityEngine;
using static Pure.DI.Lifetime;

internal partial class Composition
{
    public static readonly Composition Shared = new();

    private void Setup() => DI.Setup()
        .Bind<IClockConfig>().To((ctx) =>
        {
            ctx.Inject<ClockScope>(out var scope);
            return scope.Config;
        })
        .Bind().As(Singleton).To<ClockService>()
        .Builders<MonoBehaviour>();
}
