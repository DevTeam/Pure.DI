#pragma warning disable CS8618
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedVariable
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeNamespaceBody
namespace Pure.DI.UsageScenarios.Tests
{
    using Xunit;
    
    public class InitProperty
    {
        [Fact]
        // $visible=true
        // $tag=1 Basics
        // $priority=07
        // $description=Init property
        // {
        public void Run()
        {
            DI.Setup()
                .Bind<IService>().To<MyService>()
                .Bind<IDependency>().To<Dependency>();

            var service = InitPropertyDI.Resolve<IService>();
        }

        public class MyService: IService
        {
            [Order(0)] public IDependency Dependency { get; init; }
            
            public string State => "Some state";
        }        
        // }
    }
}
#pragma warning restore CS8618