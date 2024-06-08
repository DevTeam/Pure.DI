namespace Pure.DI.IntegrationTests;

public class TagsTests
{
    [Fact]
    public async Task ShouldSupportTags()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<string>().To(_ => "1")
                .Bind<string>(2).To(_ => "2")
                .Bind<string>('3').To(_ => "3")
                .Bind<string>("4").To(_ => "4")
                .Root<string>("Result")
                .Root<string>("Result2", 2)
                .Root<string>("Result3", '3')
                .Root<string>("Result4", "4");
        }
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            Console.WriteLine(composition.Result);
            Console.WriteLine(composition.Result2);                                           
            Console.WriteLine(composition.Result3);
            Console.WriteLine(composition.Result4);
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1", "2", "3", "4"], result);
    }
    
    [Fact]
    public async Task ShouldSupportTagsForSeveralRoots()
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
                   
            class Dependency : IDependency { }

            interface IService
            {
               IDependency Dependency1 { get; }

               IDependency Dependency2 { get; }
               
               IDependency Dependency3 { get; }
            }

            class Service : IService
            {
               public Service(
                   [Tag("Abc")] IDependency dependency1,
                   [Tag("Xyz")] IDependency dependency2,
                   IDependency dependency3)
               {
                   Dependency1 = dependency1;
                   Dependency2 = dependency2;
                   Dependency3 = dependency3;
               }

               public IDependency Dependency1 { get; }

               public IDependency Dependency2 { get; }
               
               public IDependency Dependency3 { get; }
            }

            static class Setup
            {
               private static void SetupComposition()
               {
                   DI.Setup("Composition")
                        .Bind<IDependency>("Abc", default).To<AbcDependency>()
                        .Bind<IDependency>("Xyz")
                        .As(Lifetime.Singleton)
                        .To<XyzDependency>()
                        .Root<IDependency>("XyzRoot", "Xyz")
                        .Bind<IService>().To<Service>().Root<IService>("Root");
               }
            }

            public class Program
            {
               public static void Main()
               {
                   var composition = new Composition();
                   var service = composition.Root;
                   Console.WriteLine(service.Dependency1?.GetType() == typeof(AbcDependency));
                   Console.WriteLine(service.Dependency2?.GetType() == typeof(XyzDependency));
                   Console.WriteLine(service.Dependency2 == composition.XyzRoot);
                   Console.WriteLine(service.Dependency3?.GetType() == typeof(AbcDependency));
               }
            }
        }
        """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True", "True"], result);
    }
    
    [Fact]
    public async Task ShouldSupportTagsWhenEnum()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    public enum MyEnum
    {
        Option2
    };

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<string>().To(_ => "1")
                .Bind<string>(MyEnum.Option2).To(_ => "2")
                .Root<string>("Result")
                .Root<string>("Result2", MyEnum.Option2);
        }
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            Console.WriteLine(composition.Result);                                           
            Console.WriteLine(composition.Result2);
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1", "2"], result);
    }
    
    [Fact]
    public async Task ShouldSupportTagsWhenType()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<string>().To(_ => "1")
                .Bind<string>(typeof(int)).To(_ => "2")
                .Root<string>("Result")
                .Root<string>("Result2", typeof(int));
        }
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            Console.WriteLine(composition.Result);                                           
            Console.WriteLine(composition.Result2);
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1", "2"], result);
    }
    
    [Fact]
    public async Task ShouldSupportTagsWhenNull()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<string>(1, null).To(_ => "1")
                .Bind<string>(2).To(_ => "2")
                .Root<string>("Result")
                .Root<string>("Result2", 2);
        }
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            Console.WriteLine(composition.Result);                                           
            Console.WriteLine(composition.Result2);
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1", "2"], result);
    }
    
    [Fact]
    public async Task ShouldSupportTagsWhenDefault()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<string>(1, default).To(_ => "1")
                .Bind<string>(2).To(_ => "2")
                .Root<string>("Result")
                .Root<string>("Result2", 2);
        }
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            Console.WriteLine(composition.Result);                                           
            Console.WriteLine(composition.Result2);
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1", "2"], result);
    }

#if ROSLYN4_8_OR_GREATER    
    [Fact]
    public async Task ShouldSupportTagsAsArrayInBind()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind().To(ctx => 1)
                .Bind([1, 2]).To(ctx => 2)
                .Root<int>("Root")
                .Root<int>("Root2", 1)
                .Root<int>("Root3", 2);
        }
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            Console.WriteLine(composition.Root);
            Console.WriteLine(composition.Root2);
            Console.WriteLine(composition.Root3);
        }
    }
}
""".RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp12 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1", "2", "2"], result);
    }
    
    [Fact]
    public async Task ShouldSupportTagsAsArrayInTagsMethod()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind().To(ctx => 1)
                .Bind().Tags([1, 2]).To(ctx => 2)
                .Root<int>("Root")
                .Root<int>("Root2", 1)
                .Root<int>("Root3", 2);
        }
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            Console.WriteLine(composition.Root);
            Console.WriteLine(composition.Root2);
            Console.WriteLine(composition.Root3);
        }
    }
}
""".RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp12 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1", "2", "2"], result);
    }
#endif
    
    [Fact]
    public async Task ShouldSupportTagUnique()
    {
        // Given

        // When
        var result = await """
namespace Sample
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Pure.DI;
    using Sample;
    
    internal interface IDep { }
    
    internal class Dep1: IDep { }
    
    internal class Dep2: IDep { }

    internal interface IService { }

    internal class Service: IService
    {
        public Service(IEnumerable<IDep> deps)
        {
            Console.WriteLine(deps.Count());
        }
    }
    
    internal partial class Composition
    {                   
        void Setup() => 
            DI.Setup("Composition")
                .Bind<IDep>(Tag.Unique).To<Dep1>()
                .Bind(Tag.Unique).To<Dep2>()
                .Bind<IService>().To<Service>()
                .Root<Service>("Root"); 
    }

    public class Program
    {
       public static void Main()
       {
           var composition = new Composition();
           Console.WriteLine(composition.Root);
       }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2", "Sample.Service"], result);
    }
    
    [Fact]
    public async Task ShouldSupportTagType()
    {
        // Given

        // When
        var result = await """
namespace Sample
{
    using System;
    using Pure.DI;
    using Sample;
    
    internal class Dep { }

    internal interface IService { }

    internal class Service: IService
    {
        public Service([Tag(typeof(Dep))] Dep dep) { }
    }
    
    internal partial class Composition
    {                   
        void Setup() => 
            DI.Setup("Composition")
                .Bind<Dep>(Tag.Type).To<Dep>()
                .Bind<IService>(Tag.Type).To<Service>()
                .Root<IService>("Root1", typeof(Service))
                .Root<Service>("Root2", typeof(Service)); 
    }

    public class Program
    {
       public static void Main()
       {
           var composition = new Composition();
           Console.WriteLine(composition.Root1);
           Console.WriteLine(composition.Root2);
       }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Service", "Sample.Service"], result);
    }
}