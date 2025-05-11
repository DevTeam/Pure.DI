namespace WpfAppNetCore;

using System.Windows;

internal partial class App
{
    private void OnExit(object sender, ExitEventArgs e) =>
        (TryFindResource("Composition") as IDisposable)?.Dispose();
}