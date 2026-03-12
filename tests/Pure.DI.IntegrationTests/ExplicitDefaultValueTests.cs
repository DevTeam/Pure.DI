namespace Pure.DI.IntegrationTests;

/// <summary>
/// Tests related to explicit default value handling.
/// </summary>
public class ExplicitDefaultValueTests
{
    [Fact]
    public async Task ShouldUseCtorWhenItHasDefaultValue()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IBox<out T> { T Content { get; } }
                           
                               interface ICat { }
                           
                               class CardboardBox<T> : IBox<T>
                               {
                                   public CardboardBox(T content) => Content = content;
                           
                                   public T Content { get; }
                           
                                   public override string ToString() => $"[{Content}]";
                               }
                           
                               class ShroedingersCat : ICat
                               {
                                   public ShroedingersCat(int id = 99)
                                   {
                                       Console.WriteLine(id);
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<ICat>().To<ShroedingersCat>()
                                           .Bind<IBox<TT>>().To<CardboardBox<TT>>() 
                                           .Root<Program>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   IBox<ICat> _box;
                           
                                   internal Program(IBox<ICat> box) => _box = box;
                           
                                   private void Run() { }
                           
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var program = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["99"], result);
    }

    [Fact]
    public async Task ShouldUseCtorWhenItHasDefaultValueFromOtherAssembly()
    {
        // Given

        // When
        var result = await """
                               using System;
                               using Pure.DI;

                               namespace Sample
                               {
                                   interface IDependency
                                   {
                                   }
                                   
                                   sealed class Dependency : IDependency
                                   {
                                   }
                                   
                                   interface IService
                                   {
                                       ConsoleColor Color { get; }
                                   }
                                   
                                   class Service : IService
                                   {
                                       public ConsoleColor Color { get; }
                                   
                                       public Service(IDependency dependency, ConsoleColor color = ConsoleColor.DarkBlue)
                                       {
                                           Color = color;
                                       }
                                   }
                                   
                                   static class Setup
                                   {
                                       private static void SetupComposition()
                                       {
                                           DI.Setup(nameof(Composition))
                                               .Bind().To<Dependency>()
                                               .Bind().To<Service>()
                                               .Root<IService>("Root");
                                       }
                                   }
                                       
                                   public class Program
                                   {
                                       public static void Main()
                                       {
                                           var composition = new Composition();
                                           var root = composition.Root;
                                           Console.WriteLine(root.Color);
                                       }
                                   }
                               }
                               """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["DarkBlue"], result);
    }

    [Fact]
    public async Task ShouldUseCtorWhenItHasSeveralDefaultValuesFromOtherAssembly()
    {
        // Given

        // When
        var result = await """
                               using System;
                               using Pure.DI;

                               namespace Sample
                               {
                                   interface IDependency
                                   {
                                   }
                                   
                                   sealed class Dependency : IDependency
                                   {
                                   }
                                   
                                   interface IService
                                   {
                                       ConsoleColor Color1 { get; }
                                       ConsoleColor Color2 { get; }
                                   }
                                   
                                   class Service : IService
                                   {
                                       public ConsoleColor Color1 { get; }
                                       public ConsoleColor Color2 { get; }
                                   
                                       public Service(IDependency dependency, ConsoleColor color1 = ConsoleColor.DarkBlue, ConsoleColor color2 = ConsoleColor.Red)
                                       {
                                           Color1 = color1;
                                           Color2 = color2;
                                       }
                                   }
                                   
                                   static class Setup
                                   {
                                       private static void SetupComposition()
                                       {
                                           DI.Setup(nameof(Composition))
                                               .Bind().To<Dependency>()
                                               .Bind().To<Service>()
                                               .Root<IService>("Root");
                                       }
                                   }
                                       
                                   public class Program
                                   {
                                       public static void Main()
                                       {
                                           var composition = new Composition();
                                           var root = composition.Root;
                                           Console.WriteLine(root.Color1);
                                           Console.WriteLine(root.Color2);
                                       }
                                   }
                               }
                               """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["DarkBlue", "Red"], result);
    }

    [Fact]
    public async Task ShouldUseDefaultForStructWhenExplicitDefaultValueIsNull()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               struct Counter
                               {
                                   public int Value { get; }
                                   
                                   public Counter(int value) => Value = value;
                               }

                               class Service
                               {
                                   public Service(Counter counter = default)
                                   {
                                       Counter = counter;
                                   }

                                   public Counter Counter { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Service>().To<Service>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Root;
                                       Console.WriteLine(service.Counter.Value);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["0"], result);
    }

    [Fact]
    public async Task ShouldReportMissingDependencyWhenOtherInitializerUsesExplicitDefaultValue()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Service
                               {
                                   public string First { get; private set; } = "";
                                   public string Second { get; private set; } = "";

                                   [Dependency]
                                   public void InitFirst(string value = "Default")
                                   {
                                       First = value;
                                   }

                                   [Dependency]
                                   public void InitSecond(string value)
                                   {
                                       Second = value;
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Service>().To<Service>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Root;
                                       Console.WriteLine(service.First);
                                       Console.WriteLine(service.Second);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp9, CheckCompilationErrors: false));

        // Then
        result.Success.ShouldBeFalse(result);
        result.StdOut.Any(line => line.Contains("InitSecond", StringComparison.Ordinal)).ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldUseCtorWhenItHasDayOfWeekDefaultValue()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService
                               {
                                   DayOfWeek Day { get; }
                               }
                           
                               class Service : IService
                               {
                                   public DayOfWeek Day { get; }
                               
                                   public Service(DayOfWeek day = DayOfWeek.Monday)
                                   {
                                       Day = day;
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Service>()
                                           .Root<IService>("Root");
                                   }
                               }
                               
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                       Console.WriteLine(root.Day);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Monday"], result);
    }

    [Fact]
    public async Task ShouldUseCtorWhenItHasSeveralEnumDefaultValuesFromSystem()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService
                               {
                                   ConsoleColor Color { get; }
                                   DayOfWeek Day { get; }
                               }
                           
                               class Service : IService
                               {
                                   public ConsoleColor Color { get; }
                                   public DayOfWeek Day { get; }
                               
                                   public Service(ConsoleColor color = ConsoleColor.Green, DayOfWeek day = DayOfWeek.Friday)
                                   {
                                       Color = color;
                                       Day = day;
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Service>()
                                           .Root<IService>("Root");
                                   }
                               }
                               
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                       Console.WriteLine(root.Color);
                                       Console.WriteLine(root.Day);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Green", "Friday"], result);
    }

    [Fact]
    public async Task ShouldUseCtorWhenItHasNullableEnumDefaultValue()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService
                               {
                                   ConsoleColor? Color { get; }
                               }
                           
                               class Service : IService
                               {
                                   public ConsoleColor? Color { get; }
                               
                                   public Service(ConsoleColor? color = null)
                                   {
                                       Color = color;
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Service>()
                                           .Root<IService>("Root");
                                   }
                               }
                               
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                       Console.WriteLine(root.Color.HasValue ? root.Color.Value.ToString() : "null");
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["null"], result);
    }

    [Fact]
    public async Task ShouldUseCtorWhenItHasStringComparisonDefaultValue()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService
                               {
                                   StringComparison Comparison { get; }
                               }
                           
                               class Service : IService
                               {
                                   public StringComparison Comparison { get; }
                               
                                   public Service(StringComparison comparison = StringComparison.OrdinalIgnoreCase)
                                   {
                                       Comparison = comparison;
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Service>()
                                           .Root<IService>("Root");
                                   }
                               }
                               
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                       Console.WriteLine(root.Comparison);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["OrdinalIgnoreCase"], result);
    }

    [Fact]
    public async Task ShouldUseCtorWhenItHasCustomEnumDefaultValue()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               enum MyStatus
                               {
                                   Pending,
                                   Active,
                                   Completed
                               }
                           
                               interface IService
                               {
                                   MyStatus Status { get; }
                               }
                           
                               class Service : IService
                               {
                                   public MyStatus Status { get; }
                               
                                   public Service(MyStatus status = MyStatus.Active)
                                   {
                                       Status = status;
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Service>()
                                           .Root<IService>("Root");
                                   }
                               }
                               
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                       Console.WriteLine(root.Status);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Active"], result);
    }

    [Fact]
    public async Task ShouldUseCtorWhenItHasSeveralCustomEnumDefaultValues()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               enum Priority
                               {
                                   Low,
                                   Medium,
                                   High
                               }
                               
                               enum Status
                               {
                                   Draft,
                                   Published,
                                   Archived
                               }
                           
                               interface IService
                               {
                                   Priority Priority { get; }
                                   Status Status { get; }
                               }
                           
                               class Service : IService
                               {
                                   public Priority Priority { get; }
                                   public Status Status { get; }
                               
                                   public Service(Priority priority = Priority.High, Status status = Status.Published)
                                   {
                                       Priority = priority;
                                       Status = status;
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Service>()
                                           .Root<IService>("Root");
                                   }
                               }
                               
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                       Console.WriteLine(root.Priority);
                                       Console.WriteLine(root.Status);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["High", "Published"], result);
    }

    [Fact]
    public async Task ShouldUseCtorWhenItHasNullableCustomEnumDefaultValue()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               enum MyStatus
                               {
                                   Pending,
                                   Active,
                                   Completed
                               }
                           
                               interface IService
                               {
                                   MyStatus? Status { get; }
                               }
                           
                               class Service : IService
                               {
                                   public MyStatus? Status { get; }
                               
                                   public Service(MyStatus? status = null)
                                   {
                                       Status = status;
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Service>()
                                           .Root<IService>("Root");
                                   }
                               }
                               
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                       Console.WriteLine(root.Status.HasValue ? root.Status.Value.ToString() : "null");
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["null"], result);
    }

    [Fact]
    public async Task ShouldUseCtorWhenItHasCustomEnumDefaultValueWithDependency()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency
                               {
                               }
                               
                               sealed class Dependency : IDependency
                               {
                               }
                               
                               enum LogLevel
                               {
                                   Debug,
                                   Info,
                                   Warning,
                                   Error
                               }
                           
                               interface IService
                               {
                                   LogLevel Level { get; }
                               }
                           
                               class Service : IService
                               {
                                   public LogLevel Level { get; }
                               
                                   public Service(IDependency dependency, LogLevel level = LogLevel.Warning)
                                   {
                                       Level = level;
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Dependency>()
                                           .Bind().To<Service>()
                                           .Root<IService>("Root");
                                   }
                               }
                               
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                       Console.WriteLine(root.Level);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Warning"], result);
    }
}
