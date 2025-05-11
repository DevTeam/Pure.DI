namespace WinFormsApp;

public class Program(FormMain formMain)
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        using var composition = new Composition();
        var root = composition.Root;
        root.Run();
    }

    private void Run() => Application.Run(formMain);
}