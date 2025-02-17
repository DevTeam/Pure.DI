namespace Clock.ViewModels;

using Models;

abstract class ViewModel
    : INotifyPropertyChanged
{

    public required IDispatcher Dispatcher { private get; init; }

    public required ILog<ViewModel> Log { private get; init; }
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        Log.Info($"Property \"{propertyName}\" changed");
        if (PropertyChanged is {} propertyChanged)
        {
            Dispatcher.Dispatch(() => propertyChanged(this, new PropertyChangedEventArgs(propertyName)));
        }
    }
}