namespace AvaloniaApp;

using Clock.ViewModels;
using Views;

internal class AppDataContext(
    Lazy<MainWindow> mainWindow,
    IClockViewModel clockViewModel)
{
    public MainWindow MainWindow => mainWindow.Value;

    public IClockViewModel ClockViewModel => clockViewModel;
}