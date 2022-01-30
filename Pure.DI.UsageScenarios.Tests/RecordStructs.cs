// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
namespace Pure.DI.UsageScenarios.Tests;

public class RecordStructs
{
    [Fact]
    // $visible=true
    // $tag=1 Basics
    // $priority=03
    // $description=Record structs
    // {
    public void Run()
    {
        DI.Setup()
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<RecordStructService>();

        var service = RecordStructsDI.Resolve<IService>();
        service.ShouldBeOfType<RecordStructService>();
    }

    public readonly record struct RecordStructService(IDependency Dependency, string State = "") : IService;
    // }
}