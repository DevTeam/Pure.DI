namespace Clock.Tests
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Shouldly;
    using ViewModels;

    [TestClass]
    public class ViewModelTest
    {
        [TestMethod]
        public void ShouldRaisePropertyChangedEvent()
        {
            // Given
            var model = new TestViewModel();

            // When
            model.RaiseOnPropertyChanged("SomeName1");
            model.RaiseOnPropertyChanged("SomeName2");

            // Then
            model.PropertyNames.Count.ShouldBe(2);
            model.PropertyNames.ShouldContain("SomeName1");
            model.PropertyNames.ShouldContain("SomeName2");
        }

        [TestMethod]
        public void ShouldRaisePropertyChangedEventWhenDispatcher()
        {
            // Given
            var dispatcher = new Mock<IDispatcher>();
            dispatcher.Setup(i => i.Dispatch(It.IsAny<Action>())).Callback(new Action<Action>(a => a()));
            var model = new TestViewModel(dispatcher.Object);

            // When
            model.RaiseOnPropertyChanged("SomeName");

            // Then
            model.PropertyNames.Count.ShouldBe(1);
            model.PropertyNames.ShouldContain("SomeName");
            dispatcher.Verify(i => i.Dispatch(It.IsAny<Action>()), Times.Once);
        }

        private class TestViewModel : ViewModel
        {
            private readonly List<string> _propertyNames = new List<string>();

            public TestViewModel(IDispatcher dispatcher = null) : base(dispatcher) =>
                PropertyChanged += (sender, args) => { _propertyNames.Add(args.PropertyName); };

            public IReadOnlyList<string> PropertyNames => _propertyNames.AsReadOnly();

            public void RaiseOnPropertyChanged(string propertyName = null) =>
                OnPropertyChanged(propertyName);
        }
    }
}
