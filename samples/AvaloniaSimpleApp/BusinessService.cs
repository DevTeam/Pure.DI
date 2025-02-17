namespace AvaloniaSimpleApp;

public class BusinessService : IBusinessService
{

    public BusinessService(IRepository repository) => _repository = repository;
    private readonly IRepository _repository;

    public string CreateGreetings()
    {
        _repository.RegisterSomething();
        return "Example of Dependency Injection implementation using Pure.DI";
    }
}