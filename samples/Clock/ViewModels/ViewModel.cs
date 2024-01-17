namespace Clock.ViewModels;

internal abstract class ViewModel
    : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public required IDispatcher Dispatcher { private get; init; }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (PropertyChanged is { } propertyChanged)
        {
            Dispatcher.Dispatch(() => propertyChanged(this, new PropertyChangedEventArgs(propertyName)));
        }
    }
}