// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using Xunit;
    using Pure.DI;
    using static Lifetime;

    public class Decorator
    {
        [Fact]
        // $visible=true
        // $tag=5 Interception
        // $priority=01
        // $description=Decorator
        // {
        public void Run()
        {
            DI.Setup()
                .Bind<IService>().Tags("base").To<Service>()
                .Bind<IService>().To<DecoratorService>();
            
            var service = DecoratorDI.Resolve<IService>();

            service.GetMessage().ShouldBe("Hello World !!!");
        }

        public interface IService { string GetMessage(); }

        public class Service : IService {
            public string GetMessage() => "Hello World";
        }
        
        public class DecoratorService : IService
        {
            private readonly IService _baseService;

            public DecoratorService([Tag("base")] IService baseService) => _baseService = baseService;

            public string GetMessage() => $"{_baseService.GetMessage()} !!!";
        }
        // }
    }
}
