namespace Clock;

public abstract class ViewModel
    : INotifyPropertyChanged
{
    public required IDispatcher Dispatcher { private get; init; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected TValue OnPropertyChanged<TValue>(
        TValue newPropertyValue,
        [CallerMemberName] string? propertyName = null)
    {
        if (PropertyChanged is {} propertyChanged)
        {
            Dispatcher.Dispatch(() => propertyChanged(this, new PropertyChangedEventArgs(propertyName)));
        }

        return newPropertyValue;
    }
}