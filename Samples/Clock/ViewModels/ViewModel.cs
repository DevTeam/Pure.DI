namespace Clock.ViewModels
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    internal abstract class ViewModel: INotifyPropertyChanged
    {
        private readonly IDispatcher? _dispatcher;
        public event PropertyChangedEventHandler PropertyChanged = null!;

        protected ViewModel(IDispatcher? dispatcher = null) =>
            _dispatcher = dispatcher;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            var propertyChanged = PropertyChanged;
            if (_dispatcher == null)
            {
                propertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return;
            }

            _dispatcher.Dispatch(() => propertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }
    }
}
