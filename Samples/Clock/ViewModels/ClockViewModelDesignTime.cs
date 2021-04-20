namespace Clock.ViewModels
{
    /// <summary>
    /// Design time view model.
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class ClockViewModelDesignTime: IClockViewModel
    {
        public string Time { get; } = "01:15:17";

        public string Date { get; } = "01.01.2020";
    }
}
