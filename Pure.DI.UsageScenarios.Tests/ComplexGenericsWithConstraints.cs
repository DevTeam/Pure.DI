// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedVariable
// ReSharper disable UnusedParameter.Local
namespace Pure.DI.UsageScenarios.Tests
{
    using System.Collections.Generic;
    using Xunit;

    public class ComplexGenericsWithConstraints
    {
        // $visible=true
        // $tag=1 Basics
        // $priority=10
        // $description=Complex generics with constraints
        // {
        public class Program
        {
            public Program(IConsumer<int> consumer)
            { }
        }

        public interface IConsumer<T>
        { }

        public class Consumer<T>: IConsumer<T>
        {
            public Consumer(IService<T, string, IDictionary<T, string[]>> service) { }
        }
        
        public class Consumer
        {
            public Consumer(IService<int, string, IDictionary<int, string[]>> service) { }
        }

        public interface IService<T1, T2, T3>
            where T3: IDictionary<T1, T2[]>
        { }

        public class Service<T1, T2, T3> : IService<T1, T2, T3>
            where T3: IDictionary<T1, T2[]>
        { }
        
        // }
        [Fact]
        public void Run()
        {
            // {
        DI.Setup()
            .Bind<Program>().To<Program>()
            // Bind complex generic types
            .Bind<IService<TT1, TT2, IDictionary<TT1, TT2[]>>>().To<Service<TT1, TT2, IDictionary<TT1, TT2[]>>>()
            .Bind<IConsumer<TT>>().To<Consumer<TT>>();

        // var instance = new Program(new Consumer<int>(new Service<int, string, System.Collections.Generic.IDictionary<int, string[]>>()));
        var instance = ComplexGenericsWithConstraintsDI.Resolve<Program>();
        // }
        }
    }
}
