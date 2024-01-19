namespace Clock.ViewModels;

using Models;

internal abstract class ViewModel
    : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public required IDispatcher Dispatcher { private get; init; }
    
    public required ILog<ViewModel> Log { private get; init; }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        Log.Info($"Property \"{propertyName}\" changed");
        if (PropertyChanged is { } propertyChanged)
        {
            Dispatcher.Dispatch(() => propertyChanged(this, new PropertyChangedEventArgs(propertyName)));
        }
    }
}