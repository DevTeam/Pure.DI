namespace AvaloniaSimpleApp;

public class MainViewModel : IMainViewModel
{

    public MainViewModel(IBusinessService businessService) => _businessService = businessService;
    private readonly IBusinessService _businessService;

    public string Title => "Avalonia application";

    public string Greetings => _businessService.CreateGreetings();
}