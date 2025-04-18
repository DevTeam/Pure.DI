namespace AvaloniaSimpleApp;

public class BusinessService : IBusinessService
{
    private readonly IRepository _repository;

    public BusinessService(IRepository repository) => _repository = repository;

    public string CreateGreetings()
    {
        _repository.RegisterSomething();
        return "Example of Dependency Injection implementation using Pure.DI";
    }
}