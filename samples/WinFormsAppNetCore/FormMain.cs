namespace WinFormsAppNetCore;

using System.ComponentModel;
using System.Windows.Forms;
using Clock.ViewModels;

internal partial class FormMain : Form
{
    private readonly IClockViewModel _clockViewModel;

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

    private void UpdateDateTime()
    {
        labelDate.Text = _clockViewModel.Date;
        labelTime.Text = _clockViewModel.Time;
    }
}