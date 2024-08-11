namespace AvaloniaApp;

using Clock.ViewModels;
using Views;

internal record Root(
    Lazy<MainWindow> MainWindow,
    IClockViewModel ClockViewModel);