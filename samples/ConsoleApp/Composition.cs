using static Pure.DI.Tag;
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable HeapView.PossibleBoxingAllocation

namespace ConsoleApp;

using System.Diagnostics.CodeAnalysis;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
partial class Composition
{
    [Conditional("DI")]
    private void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "Off")
        .Hint(Hint.ThreadSafe, "Off")

#if DEBUG
        .Hint(Hint.ToString, "On")
        .Hint(Hint.OnNewInstance, "On")
        .Hint(Hint.OnDependencyInjection, "On")
#endif

        // Composition root for the console application
        .Root<Program>(nameof(Root))
        .Arg<TimeSpan>("period")

        .Bind().To<ClockModel>()
        .Bind(Utc).To<UtcClockModel>()
        .Bind().As(Singleton).To<Ticks>()
        .Bind<IConsole>().To<ConsoleAdapter>()

        // Infrastructure
        .Bind().To<DebugLog<TT>>();

#if DEBUG
    partial void OnNewInstance<T>(ref T value, object? tag, Lifetime lifetime)
    {
        Debug.WriteLine($"DI: {lifetime} {value}(#{value?.GetHashCode()}) of type {typeof(T).Name} created.");
    }

    private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime)
    {
        Debug.WriteLine($"DI: {value}(#{value?.GetHashCode()}) injected as {typeof(T).Name}.");
        return value;
    }
#endif
}