namespace WinFormsAppNetCore;

using System.ComponentModel;
using Clock.ViewModels;

partial class FormMain : Form
{

    internal FormMain(IClockViewModel clockViewModel)
    {
        _clockViewModel = clockViewModel;
        InitializeComponent();
        UpdateDateTime();
        if (clockViewModel is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += (_, _) => UpdateDateTime();
        }
    }
    private readonly IClockViewModel _clockViewModel;

    private void UpdateDateTime()
    {
        labelDate.Text = _clockViewModel.Date;
        labelTime.Text = _clockViewModel.Time;
    }
}