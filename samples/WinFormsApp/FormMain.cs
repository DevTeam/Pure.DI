namespace WinFormsApp;

using System.ComponentModel;

public partial class FormMain : Form
{
    private readonly IAppViewModel _appViewModel;
    private readonly IClockViewModel _clockViewModel;

    internal FormMain(IAppViewModel appViewModel, IClockViewModel clockViewModel)
    {
        _appViewModel = appViewModel;
        _clockViewModel = clockViewModel;
        InitializeComponent();
        UpdateDateTime();
        if (clockViewModel is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += (_, _) => UpdateDateTime();
        }
    }

    private void UpdateDateTime()
    {
        Text = _appViewModel.Title;
        labelDate.Text = _clockViewModel.Date;
        labelTime.Text = _clockViewModel.Time;
    }
}