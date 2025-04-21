namespace Clock.Tests;

using System.Collections.ObjectModel;
using Models;

public class ViewModelTest
{
    private readonly Mock<IDispatcher> _dispatcher = new();

    public ViewModelTest() =>
        _dispatcher.Setup(i => i.Dispatch(It.IsAny<Action>()))
            .Callback<Action>(action => action());

    private class TestViewModel : ViewModel
    {
        private readonly List<string?> _propertyNames = [];

        public TestViewModel() =>
            PropertyChanged += (_, args) => { _propertyNames.Add(args.PropertyName); };

        public ReadOnlyCollection<string?> PropertyNames => _propertyNames.AsReadOnly();

        public void RaiseOnPropertyChanged(string? propertyName = null) =>
            OnPropertyChanged(propertyName);
    }

    [Fact]
    public void ShouldRaisePropertyChangedEvent()
    {
        // Given
        var model = new TestViewModel
        {
            Dispatcher = _dispatcher.Object,
            Log = Mock.Of<ILog<ViewModel>>()
        };

        // When
        model.RaiseOnPropertyChanged("SomeName");

        // Then
        model.PropertyNames.Count.ShouldBe(1);
        model.PropertyNames.ShouldContain("SomeName");
        _dispatcher.Verify(i => i.Dispatch(It.IsAny<Action>()), Times.Once);
    }
}