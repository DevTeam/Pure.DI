// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable ClassNeverInstantiated.Local
namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using System.Collections.Generic;
    using Xunit;

    public class GenericAutowiring
    {
        [Fact]
        // $visible=true
        // $tag=1 Basics
        // $priority=06
        // $description=Generic autowiring
        // $header=Autowiring of generic types as simple as autowiring of other simple types. Just use a generic parameters markers like _TT_, _TT1_, _TT2_ and etc. or TTI, TTI1, TTI2 ... for interfaces or TTS, TTS1, TTS2 ... for value types or other special markers like TTDisposable, TTDisposable1 and etc. TTList<>, TTDictionary<> ... or create your own generic parameters markers or bind open generic types.
        // {
        public void Run()
        {
            // Create and configure the container using autowiring
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                // Bind using the predefined generic parameters marker TT (or TT1, TT2, TT3 ...)
                .Bind<IService<TT>>().To<Service<TT>>()
                // Bind using the predefined generic parameters marker TTList (or TTList1, TTList2 ...)
                // For other cases there are TTComparable, TTComparable<in T>, TTEquatable<T>, TTEnumerable<out T>, TTDictionary<TKey, TValue> and etc.
                .Bind<IListService<TTList<int>>>().To<ListService<TTList<int>>>()
                // Bind using the custom generic parameters marker TCustom
                .Bind<IService<TTMy>>().Tag("custom marker").To<Service<TTMy>>()
                .Bind<CompositionRoot<IListService<IList<int>>>>().To<CompositionRoot<IListService<IList<int>>>>()
                .Bind<CompositionRoot<ICollection<IService<int>>>>().To<CompositionRoot<ICollection<IService<int>>>>();

            // Resolve a generic instance
            var listService = GenericAutowiringDI.Resolve<CompositionRoot<IListService<IList<int>>>>().Root;
            var instances = GenericAutowiringDI.Resolve<CompositionRoot<ICollection<IService<int>>>>().Root;

            instances.Count.ShouldBe(2);
            // Check the instance's type
            foreach (var instance in instances)
            {
                instance.ShouldBeOfType<Service<int>>();
            }

            listService.ShouldBeOfType<ListService<IList<int>>>();
        }

        // Custom generic type marker using predefined attribute `GenericTypeArgument`
        [GenericTypeArgument]
        class TTMy { }
        // }
    }
}
