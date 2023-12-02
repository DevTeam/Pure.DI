// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Global
namespace Pure.DI.Benchmarks.Benchmarks;

internal class DefaultComposition
{
    private static void Setup() =>
        DI.Setup(nameof(DefaultComposition), CompositionKind.Global)
            .Hint(Hint.ThreadSafe, "Off")
            .Hint(Hint.ToString, "On")
            .Hint(Hint.FormatCode, "On")
            .Bind<Func<TT>>()
                .As(Lifetime.PerBlock)
                .To(ctx => new Func<TT>(() =>
                {
                    ctx.Inject<TT>(ctx.Tag, out var value);
                    return value;
                }));
}