// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using Xunit;
    using Pure.DI;

    public class Records
    {
        [Fact]
        // $visible=true
        // $tag=1 Basics
        // $priority=03
        // $description=Records
        // {
        public void Run()
        {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<RecordService>();
            
            var service = RecordsDI.Resolve<IService>();
            service.ShouldBeOfType<RecordService>();
        }

        public record RecordService(IDependency Dependency, string State = "") : IService;
        // }
    }
}
