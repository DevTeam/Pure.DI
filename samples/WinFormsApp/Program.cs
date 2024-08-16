using System;
using System.Windows.Forms;

namespace WinFormsApp
{
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
            using var composition = new Composition();
            using var root = composition.Root;
            Application.Run(root.Value);
        }
    }
}