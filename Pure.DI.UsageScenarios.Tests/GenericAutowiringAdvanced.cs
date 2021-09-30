// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using System.Collections.Generic;
    using Xunit;

    public class GenericAutowiringAdvanced
    {
        [Fact]
        // $visible=true
        // $tag=1 Basics
        // $priority=10
        // $description=Advanced generic autowiring
        // $header=Autowiring of generic types as simple as autowiring of other simple types. Just use a generic parameters markers like _TT_, _TT1_, _TT2_ and etc. or TTI, TTI1, TTI2 ... for interfaces or TTS, TTS1, TTS2 ... for value types or other special markers like TTDisposable, TTDisposable1 and etc. TTList<>, TTDictionary<> ... or create your own generic parameters markers or bind open generic types.
        // $footer=This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).
        // {
        public void Run()
        {
            // Create and configure the container using autowiring
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                // Bind using the predefined generic parameters marker TT (or TT1, TT2, TT3 ...)
                .Bind<IService<TT>>().To<Service<TT>>()
                // Bind using the predefined generic parameters marker TTList (or TTList1, TTList2 ...)
                // For other cases there are TTComparable, TTComparable<in T>,
                // TTEquatable<T>, TTEnumerable<out T>, TTDictionary<TKey, TValue> and etc.
                .Bind<IListService<TTList<int>>>().To<ListService<TTList<int>>>()
                // Bind using the custom generic parameters marker TCustom
                .Bind<IService<TTMy>>().Tags("custom tag").To<Service<TTMy>>()
                .Bind<Consumer>().To<Consumer>();

            // Resolve a generic instance
            var consumer = GenericAutowiringAdvancedDI.Resolve<Consumer>();
            
            consumer.Services2.Count.ShouldBe(2);
            // Check the instance's type
            foreach (var instance in consumer.Services2)
            {
                instance.ShouldBeOfType<Service<int>>();
            }

            consumer.Services1.ShouldBeOfType<ListService<IList<int>>>();
        }

        public class Consumer
        {
            public Consumer(IListService<IList<int>> services1, ICollection<IService<int>> services2)
            {
                Services1 = services1;
                Services2 = services2;
            }
            
            public IListService<IList<int>> Services1 { get; }

            public ICollection<IService<int>> Services2 { get; }
        }

        // Custom generic type marker using predefined attribute `GenericTypeArgument`
        [GenericTypeArgument]
        class TTMy { }
        // }
    }
}
