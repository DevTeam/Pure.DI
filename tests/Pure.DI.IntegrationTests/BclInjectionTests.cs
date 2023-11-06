namespace Pure.DI.IntegrationTests;

[Collection(nameof(NonParallelTestsCollectionDefinition))]
public class BclInjectionTests
{
    [Theory]
    [InlineData("System.Collections.Generic.IList")]
    [InlineData("System.Collections.Immutable.ImmutableArray")]
    [InlineData("System.Collections.Generic.IReadOnlyList")]
    [InlineData("System.Collections.Generic.ISet")]
    [InlineData("System.Collections.Generic.HashSet")]
    [InlineData("System.Collections.Generic.SortedSet")]
    [InlineData("System.Collections.Generic.Queue")]
    [InlineData("System.Collections.Generic.Stack")]
    [InlineData("System.Collections.Immutable.IImmutableList")]
    [InlineData("System.Collections.Immutable.ImmutableList")]
    [InlineData("System.Collections.Immutable.IImmutableSet")]
    [InlineData("System.Collections.Immutable.ImmutableHashSet")]
    [InlineData("System.Collections.Immutable.ImmutableSortedSet")]
    [InlineData("System.Collections.Immutable.IImmutableQueue")]
    [InlineData("System.Collections.Immutable.ImmutableQueue")]
    [InlineData("System.Collections.Immutable.IImmutableStack")]
    [InlineData("System.Collections.Immutable.ImmutableStack")]
    [InlineData("System.Span")]
    [InlineData("System.Span", LanguageVersion.CSharp8)]
    [InlineData("System.ReadOnlySpan")]
    [InlineData("System.Memory")]
    [InlineData("System.ReadOnlyMemory")]
    public async Task ShouldSupportCollectionInjection(string collectionType, LanguageVersion languageVersion = LanguageVersion.CSharp9)
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    struct Point
    {
        int X, Y;        
        
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
    } 

    interface IDependency {}

    class Dependency: IDependency, IComparable<Dependency>, IComparable
    {        
        public Dependency(ReadOnlySpan<Point> points1, Span<Point> points2)
        {
            Console.WriteLine("Dependency created");
        }

        public int CompareTo(Dependency other) => GetHashCode() - other.GetHashCode();
        
        public int CompareTo(object obj) => GetHashCode() - obj.GetHashCode();
    }

    interface IService
    {                    
    }

    class Service: IService 
    {
        public Service(###CollectionType###<IDependency> deps)
        { 
            Console.WriteLine("Service creating");            
        }                            
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            // FormatCode = On
            DI.Setup("Composition")
                .Bind<IDependency>(1).To<Dependency>()
                .Bind<IDependency>(2).To<Dependency>()
                .Bind<IDependency>(3).To<Dependency>()
                .Bind<Point>(1).To(_ => new Point(1, 2))
                .Bind<Point>(2).To(_ => new Point(2, 3))
                .Bind<IService>().To<Service>()
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            try 
            {
                var composition = new Composition();
                var service = composition.Service;
            }                                     
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }                
        }
    }                
}
""".Replace("###CollectionType###", collectionType).RunAsync(
            new Options
            {
                LanguageVersion = languageVersion,
                NullableContextOptions = NullableContextOptions.Disable
            });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("Dependency created", "Dependency created", "Dependency created", "Service creating"), result);
    }
    
    [Theory]
    [InlineData("System.Collections.Generic.IList")]
    [InlineData("System.Collections.Immutable.ImmutableArray")]
    [InlineData("System.Collections.Generic.IReadOnlyList")]
    [InlineData("System.Collections.Generic.ISet")]
    [InlineData("System.Collections.Generic.HashSet")]
    [InlineData("System.Collections.Generic.SortedSet")]
    [InlineData("System.Collections.Generic.Queue")]
    [InlineData("System.Collections.Generic.Stack")]
    [InlineData("System.Collections.Immutable.IImmutableList")]
    [InlineData("System.Collections.Immutable.ImmutableList")]
    [InlineData("System.Collections.Immutable.IImmutableSet")]
    [InlineData("System.Collections.Immutable.ImmutableHashSet")]
    [InlineData("System.Collections.Immutable.ImmutableSortedSet")]
    [InlineData("System.Collections.Immutable.IImmutableQueue")]
    [InlineData("System.Collections.Immutable.ImmutableQueue")]
    [InlineData("System.Collections.Immutable.IImmutableStack")]
    [InlineData("System.Collections.Immutable.ImmutableStack")]
    [InlineData("System.Span")]
    [InlineData("System.Span", LanguageVersion.CSharp8)]
    [InlineData("System.ReadOnlySpan")]
    [InlineData("System.Memory")]
    [InlineData("System.ReadOnlyMemory")]
    public async Task ShouldSupportCollectionInjectionWhenHasNoBindings(string collectionType, LanguageVersion languageVersion = LanguageVersion.CSharp9)
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    struct Point
    {
        int X, Y;        
        
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
    } 

    interface IDependency {}

    class Dependency: IDependency, IComparable<Dependency>, IComparable
    {        
        public Dependency(ReadOnlySpan<Point> points1, Span<Point> points2)
        {
            Console.WriteLine("Dependency created");
        }

        public int CompareTo(Dependency other) => GetHashCode() - other.GetHashCode();
        
        public int CompareTo(object obj) => GetHashCode() - obj.GetHashCode();
    }

    interface IService
    {                    
    }

    class Service: IService 
    {
        public Service(###CollectionType###<IDependency> deps)
        { 
            Console.WriteLine("Service creating");            
        }                            
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<Point>(1).To(_ => new Point(1, 2))
                .Bind<Point>(2).To(_ => new Point(2, 3))
                .Bind<IService>().To<Service>()
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            try 
            {
                var composition = new Composition();
                var service = composition.Service;
            }                                     
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }                
        }
    }                
}
""".Replace("###CollectionType###", collectionType).RunAsync(
            new Options
            {
                LanguageVersion = languageVersion,
                NullableContextOptions = NullableContextOptions.Disable
            });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("Service creating"), result);
    }
    
    [Theory]
    [InlineData("System.Collections.Generic.IAsyncEnumerable")]
    public async Task ShouldSupportAsyncCollectionInjection(string collectionType, LanguageVersion languageVersion = LanguageVersion.CSharp9)
    {
        // Given

        // When
        var result = await """
using System;
using System.Threading.Tasks;
using Pure.DI;

namespace Sample
{
    struct Point
    {
        int X, Y;        
        
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
    } 

    interface IDependency {}

    class Dependency: IDependency, IComparable<Dependency>, IComparable
    {        
        public Dependency(Point[] points1, System.Collections.Generic.IList<Point> points2)
        {
            Console.WriteLine("Dependency created");
        }

        public int CompareTo(Dependency other) => GetHashCode() - other.GetHashCode();
        
        public int CompareTo(object obj) => GetHashCode() - obj.GetHashCode();
    }

    interface IService
    {                    
    }

    class Service: IService 
    {
        public Service(###CollectionType###<IDependency> deps)
        { 
            foreach (var dependency in deps.ToBlockingEnumerable())
            {
            }

            Console.WriteLine("Service creating");            
        }                            
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            // FormatCode = On
            DI.Setup("Composition")
                .Bind<IDependency>(1).To<Dependency>()
                .Bind<IDependency>(2).To<Dependency>()
                .Bind<IDependency>(3).To<Dependency>()
                .Bind<Point>(1).To(_ => new Point(1, 2))
                .Bind<Point>(2).To(_ => new Point(2, 3))
                .Bind<IService>().To<Service>()
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            try 
            {
                var composition = new Composition();
                var service = composition.Service;
            }                                     
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }                
        }
    }                
}
""".Replace("###CollectionType###", collectionType).RunAsync(
            new Options
            {
                LanguageVersion = languageVersion,
                NullableContextOptions = NullableContextOptions.Disable
            });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("Dependency created", "Dependency created", "Dependency created", "Service creating"), result);
    }
}