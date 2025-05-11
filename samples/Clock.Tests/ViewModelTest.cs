namespace Clock.Tests;

public class ViewModelTest
{
    private readonly Mock<IDispatcher> _dispatcher = new();
    private readonly TestViewModel _model;

    public ViewModelTest()
    {
        _dispatcher.Setup(i => i.Dispatch(It.IsAny<Action>())).Callback<Action>(action => action());
        _model = new TestViewModel { Dispatcher = _dispatcher.Object };
    }

    [Fact]
    public void ShouldDispatchPropertyChangedEvents()
    {
        // Given

        // When
        _model.RaiseOnPropertyChanged("Abc", "SomeName");

        // Then
        _dispatcher.Verify(i => i.Dispatch(It.IsAny<Action>()), Times.Once);
    }

    [Fact]
    public void ShouldRaisePropertyChangedEvent()
    {
        // Given

        // When
        _model.RaiseOnPropertyChanged("Abc", "SomeName");

        // Then
        _model.PropertyNames.ShouldContain("SomeName");
    }

    private class TestViewModel : ViewModel
    {
        private readonly List<string?> _propertyNames = [];

        public TestViewModel()
        {
            PropertyChanged += (_, args) => { _propertyNames.Add(args.PropertyName); };
        }

        public IReadOnlyCollection<string?> PropertyNames => _propertyNames;

        public void RaiseOnPropertyChanged(string newPropertyValue, string? propertyName = null) =>
            OnPropertyChanged(newPropertyValue, propertyName);
    }
}