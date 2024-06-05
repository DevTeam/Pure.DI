namespace Pure.DI.IntegrationTests;

public class AttributeTests
{
    [Fact]
    public async Task ShouldSupportTypeAttribute()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    interface IDependency { }

    class AbcDependency : IDependency { }

    class XyzDependency : IDependency { }

    interface IService
    {
        IDependency Dependency1 { get; }
        
        IDependency Dependency2 { get; }
    }

    class Service : IService
    {
        public Service(
            [Type(typeof(AbcDependency))] IDependency dependency1,
            [Type(typeof(XyzDependency))] IDependency dependency2)
        {
            Dependency1 = dependency1;
            Dependency2 = dependency2;
        }

        public IDependency Dependency1 { get; }

        public IDependency Dependency2 { get; }
    }

    partial class Composition
    {
        private static void SetupComposition()
        {
            DI.Setup(nameof(Composition))
                .Bind<IService>().To<Service>()

                // Composition root
                .Root<IService>("Root");
        }
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service = composition.Root;
            Console.WriteLine(service.Dependency1);
            Console.WriteLine(service.Dependency2);
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.AbcDependency", "Sample.XyzDependency"], result);
    }
    
#if ROSLYN4_8_OR_GREATER    
    [Fact]
    public async Task ShouldSupportGenericTypeAttribute()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    internal class TypeAttribute<T> : Attribute
    {
    }

    interface IDependency { }

    class AbcDependency : IDependency { }

    class XyzDependency : IDependency { }

    interface IService
    {
        IDependency Dependency1 { get; }
        
        IDependency Dependency2 { get; }
    }

    class Service : IService
    {
        public Service(
            [Type<AbcDependency>] IDependency dependency1,
            [Type<XyzDependency>] IDependency dependency2)
        {
            Dependency1 = dependency1;
            Dependency2 = dependency2;
        }

        public IDependency Dependency1 { get; }

        public IDependency Dependency2 { get; }
    }

    partial class Composition
    {
        private static void SetupComposition()
        {
            DI.Setup(nameof(Composition))
                .TypeAttribute<TypeAttribute<TT>>()
                .Bind<IService>().To<Service>()

                // Composition root
                .Root<IService>("Root");
        }
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service = composition.Root;
            Console.WriteLine(service.Dependency1);
            Console.WriteLine(service.Dependency2);
        }
    }
}
""".RunAsync(new Options(LanguageVersion.CSharp11));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.AbcDependency", "Sample.XyzDependency"], result);
    }
    
    [Fact]
    public async Task ShouldSupportCombinedGenericAttribute()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    [AttributeUsage(
        AttributeTargets.Constructor
        | AttributeTargets.Method
        | AttributeTargets.Parameter
        | AttributeTargets.Property
        | AttributeTargets.Field)]
    class InjectAttribute<T> : Attribute
    {
        public InjectAttribute(object? tag = null, int ordinal = 0)
        {
        }
    }

    interface IPerson {}

    class Person : IPerson
    {
        private object? _state;
        
        [Inject<int>]
        internal object Id = "";

        private readonly string _name;

        public Person([Inject<string>("NikName")] string name)
        {
            _name = name;
        }

        public void Initialize([Inject<Uri>] object state) => 
            _state = state;

        public override string ToString() => $"{Id} {_name} {_state}";
    }

    partial class Composition
    {
        private static void SetupComposition()
        {
            DI.Setup(nameof(Composition))
                .TagAttribute<InjectAttribute<TT>>(0)
                .OrdinalAttribute<InjectAttribute<TT>>(1)
                .TypeAttribute<InjectAttribute<TT>>()
                .Arg<int>("personId")
                .Bind().To(_ => new Uri("https://github.com/DevTeam/Pure.DI"))
                .Bind("NikName").To(_ => "Nik")
                .Bind().To<Person>()

                // Composition root
                .Root<IPerson>("Person");
        }
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition(personId: 123);
            var person = composition.Person;
            Console.WriteLine(person);
        }
    }
}
""".RunAsync(new Options(LanguageVersion.CSharp11));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["123 Nik https://github.com/DevTeam/Pure.DI"], result);
    }
#endif
}