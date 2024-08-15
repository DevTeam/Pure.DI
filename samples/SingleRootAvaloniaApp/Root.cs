namespace AvaloniaApp;

using Clock.ViewModels;
using Pure.DI;
using Views;

internal readonly record struct Root(
    IOwned Owned,
    Func<MainWindow> CreateMainWindow,
    IClockViewModel ClockViewModel);