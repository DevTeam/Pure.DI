// ReSharper disable RedundantNameQualifier
// ReSharper disable ArrangeNamespaceBody
namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Xunit;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class MultiStatementFunc
    {
        [Fact]
        // $visible=true
        // $tag=3 BCL types
        // $priority=03
        // $description=Multi statement func
        // {
        public void Run()
        {
            DI.Setup()
                .Bind<MultiStatementFunc.MyService>().To<MultiStatementFunc.MyService>()
                .Bind<Func<string, MultiStatementFunc.IMyService>>().To(ctx => new Func<string, MultiStatementFunc.IMyService>(name =>
                {
                    var service = ctx.Resolve<MyService>();
                    service.Name = name;
                    return service;
                }))
                .Bind<Consumer>().To<Consumer>();

            // Resolve function to create instances
            var consumer = MultiStatementFuncDI.Resolve<Consumer>();

            consumer.Service.Name.ShouldBe("Abc");
        }

        public interface IMyService
        {
            string Name { get; }
        }

        public class MyService: IMyService
        {
            public string Name { get; set; } = "";
        }

        public class Consumer
        {
            public Consumer(Func<string, IMyService> factory) => Service = factory("Abc");

            public IMyService Service { get; }
        }
        // }
        // Check each instance
    }
}
