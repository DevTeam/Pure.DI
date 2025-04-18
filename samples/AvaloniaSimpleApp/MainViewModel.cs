namespace AvaloniaSimpleApp;

public class MainViewModel : IMainViewModel
{
    private readonly IBusinessService _businessService;

    public MainViewModel(IBusinessService businessService) => _businessService = businessService;

    public string Title => "Avalonia application";

    public string Greetings => _businessService.CreateGreetings();
}