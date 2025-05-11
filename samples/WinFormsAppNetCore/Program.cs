namespace WinFormsAppNetCore;

public class Program(FormMain formMain)
{
    [STAThread]
    public static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var composition = new Composition();
        composition.Root.Run();
    }

    private void Run() => Application.Run(formMain);
}