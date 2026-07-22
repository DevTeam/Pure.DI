using Uno.UI.Hosting;
// ReSharper disable CheckNamespace

namespace UnoApp;

static class Program
{
    [STAThread]
    public static void Main()
    {
        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWin32()
            .Build();

        host.Run();
    }
}
