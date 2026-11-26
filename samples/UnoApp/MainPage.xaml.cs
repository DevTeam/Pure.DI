// ReSharper disable RedundantExtendsListEntry
// The XAML part already specifies the base type, but the Uno XAML compiler requires it in the code-behind too (UXAML0002).
namespace UnoApp;

public sealed partial class MainPage : Page
{
    public MainPage() => InitializeComponent();
}
