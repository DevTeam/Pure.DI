namespace AvaloniaSimpleApp;

public class MainViewModel(IBusinessService businessService)
    : IMainViewModel
{
    public string Title => "Avalonia application";

    public string Greetings => businessService.CreateGreetings();
}