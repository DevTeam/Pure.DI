// ReSharper disable RedundantUsingDirective
// ReSharper disable ArrangeNamespaceBody
namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using System.Diagnostics.CodeAnalysis;
    using Xunit;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class Tuples
    {
        [Fact]
        public void Run()
        {
            // $visible=true
            // $tag=3 BCL types
            // $priority=01
            // $description=Tuples
            // $header=[Tuple](https://docs.microsoft.com/en-us/dotnet/api/system.tuple) has a set of elements that should be resolved at the same time.
            // $footer=This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>()
                .Bind<INamedService>().To(ctx => new NamedService(ctx.Resolve<IDependency>(), "some name"))
                .Bind<CompositionRoot<(IService, INamedService)>>().To<CompositionRoot<(IService, INamedService)>>();

            // Resolve an instance of type Tuple<IService, INamedService>
            var (service, namedService) = TuplesDI.Resolve<CompositionRoot<(IService, INamedService)>>().Root;
            // }
            // Check items
            service.ShouldBeOfType<Service>();
            namedService.ShouldBeOfType<NamedService>();
        }
    }
}
