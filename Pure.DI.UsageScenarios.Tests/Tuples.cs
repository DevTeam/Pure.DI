namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using System;
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
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>()
                .Bind<INamedService>().To(ctx => new NamedService(ctx.Resolve<IDependency>(), "some name"))
                .Bind<CompositionRoot<Tuple<IService, INamedService>>>().To<CompositionRoot<Tuple<IService, INamedService>>>();

            // Resolve an instance of type Tuple<IService, INamedService>
            var (service, namedService) = TuplesDI.Resolve<CompositionRoot<Tuple<IService, INamedService>>>().Root;
            // }
            // Check items
            service.ShouldBeOfType<Service>();
            namedService.ShouldBeOfType<NamedService>();
        }
    }
}
