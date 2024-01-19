namespace Clock.Tests;

using System.Collections.Generic;
using Models;
using ViewModels;

[TestClass]
public class ViewModelTest
{
    private readonly  Mock<IDispatcher> _dispatcher = new();

    public ViewModelTest() =>
        _dispatcher.Setup(i => i.Dispatch(It.IsAny<Action>()))
            .Callback<Action>(action => action());

    [TestMethod]
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

    private class TestViewModel : ViewModel
    {
        private readonly List<string?> _propertyNames = [];

        public TestViewModel() =>
            PropertyChanged += (_, args) => { _propertyNames.Add(args.PropertyName); };

        public IReadOnlyList<string?> PropertyNames => _propertyNames.AsReadOnly();

        public void RaiseOnPropertyChanged(string? propertyName = null) =>
            OnPropertyChanged(propertyName);
    }
}