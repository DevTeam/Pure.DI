namespace Pure.DI.UsageTests;

interface IService;

class Service : IService;

public class Abc
{
    public static void Setup() =>
        // Represents the composition class
        DI.Setup(nameof(Composition))
            .Bind<IService>().To<Service>()
            // Provides a composition root of my service
            .Root<IService>("MyService");

    public Abc()
    {
        var composition = new Composition();
        var service = composition.MyService;
    }
}