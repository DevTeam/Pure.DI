namespace Clock.ViewModels;

internal abstract class ViewModel(IDispatcher? dispatcher = null)
    : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        var propertyChanged = PropertyChanged;
        if (propertyChanged == null)
        {
            return;
        }

        if (dispatcher == null)
        {
            propertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return;
        }

        dispatcher.Dispatch(() => propertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName)));
    }
}