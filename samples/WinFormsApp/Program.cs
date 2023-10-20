using System;
using System.Windows.Forms;

namespace WinFormsApp;

public static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        var composition = new Composition();
        Application.Run(composition.FormMain);
    }
}