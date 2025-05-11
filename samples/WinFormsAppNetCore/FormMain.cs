using System.Collections.Concurrent;

namespace WinFormsAppNetCore;

public partial class FormMain : Form
{
    private readonly List<INotifyPropertyChanged> _viewModels;
    private readonly ConcurrentDictionary<string, Binding> _bindings = new();

    internal FormMain(IAppViewModel app, IClockViewModel clock)
    {
        _viewModels = ((object[])[app, clock]).OfType<INotifyPropertyChanged>().ToList();
        _viewModels.ForEach(vm => vm.PropertyChanged += OnPropertyChanged);
        InitializeComponent();
        DataBindings.Add(CreateBinding(app, nameof(IAppViewModel.Title)));
        labelDate.DataBindings.Add(CreateBinding(clock, nameof(clock.Date)));
        labelTime.DataBindings.Add(CreateBinding(clock, nameof(clock.Time)));
    }

    protected override void OnClosing(CancelEventArgs e) =>
        _viewModels.ForEach(vm => vm.PropertyChanged -= OnPropertyChanged);

    private Binding CreateBinding(object model, string propertyName, string targetName = nameof(Text)) =>
        _bindings.GetOrAdd(
            propertyName,
            static (propertyName, s) => new Binding(s.targetName, s.model, propertyName),
            (targetName, model));

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is {} name
            && _bindings.TryGetValue(name, out var binding))
        {
            binding.ReadValue();
        }
    }
}