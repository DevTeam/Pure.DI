namespace AvaloniaApp;

using Pure.DI;
using Views;

readonly record struct Root(
    IOwned Owned,
    Func<MainWindow> CreateMainWindow,
    IAppViewModel App,
    IClockViewModel Clock);