namespace AvaloniaSimpleApp;

public class BusinessService(IRepository repository)
    : IBusinessService
{
    public string CreateGreetings()
    {
        repository.RegisterSomething();
        return "Example of Dependency Injection implementation using Pure.DI";
    }
}