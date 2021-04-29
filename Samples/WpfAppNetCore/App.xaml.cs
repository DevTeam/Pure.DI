namespace WpfAppNetCore
{
    using System.Windows;
    using Views;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ClockDomain.Resolve<IMainWindowView>().Show();
        }
    }
}
