namespace Pure.DI.IntegrationTests;

public class SimpleFactoryTests
{

    [Theory]
    [InlineData("global::System.Collections.Generic.", "Pure.DI.")]
    [InlineData("System.Collections.Generic.", "Pure.DI.")]
    [InlineData("", "Pure.DI.")]
    [InlineData("global::System.Collections.Generic.", "global::Pure.DI.")]
    [InlineData("System.Collections.Generic.", "global::Pure.DI.")]
    [InlineData("", "global::Pure.DI.")]
    [InlineData("global::System.Collections.Generic.", "")]
    [InlineData("System.Collections.Generic.", "")]
    [InlineData("", "")]
    public async Task ShouldSupportSimpleFactoryWhenArrOfT(string typePrefix, string ttPrefix)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency {}
                           
                               interface IService
                               {
                                   IDependency? Dep { get; }
                                   
                                   IService Initialize(IDependency dep);
                               }
                           
                               class Service: IService 
                               {
                                   public IDependency? Dep { get; private set; }
                                   
                                   public IService Initialize(IDependency dep)
                                   {
                                       Dep = dep;
                                       return this;
                                   }
                                   
                                   public override string ToString()
                                   {
                                       return Dep?.ToString() ?? "";
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<#TypePrefixICollection<#ttPrefixTT>>()
                                           .Bind<#TypePrefixIList<#ttPrefixTT>>()
                                           .Bind<#TypePrefixList<#ttPrefixTT>>()
                                               .To((#ttPrefixTT[] arr) => new #TypePrefixList<#ttPrefixTT>(arr))
                                           .Bind().To<Dependency>()
                                           .Bind().To<Service>()
                                           .Bind<string>().To((IService service, #TypePrefixIList<IDependency> dependency) => service.Initialize(dependency[0]).ToString() ?? "")
                                           .Root<string>("DepName");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var depName = composition.DepName;
                                       Console.WriteLine(depName);
                                   }
                               }
                           }
                           """.Replace("#TypePrefix", typePrefix).Replace("#ttPrefix", ttPrefix).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Dependency"], result);
    }
    [Fact]
    public async Task ShouldSupportSimpleFactory()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency {}
                           
                               interface IService
                               {
                                   IDependency? Dep { get; }
                                   
                                   IService Initialize(IDependency dep);
                               }
                           
                               class Service: IService 
                               {
                                   public IDependency? Dep { get; private set; }
                                   
                                   public IService Initialize(IDependency dep)
                                   {
                                       Dep = dep;
                                       return this;
                                   }
                                   
                                   public override string ToString()
                                   {
                                       return Dep?.ToString() ?? "";
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind().To<Service>()
                                           .Bind<string>().To<IService, IDependency, string>((service, dependency) => service.Initialize(dependency).ToString() ?? "")
                                           .Root<string>("DepName");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var depName = composition.DepName;
                                       Console.WriteLine(depName);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Dependency"], result);
    }

    [Fact]
    public async Task ShouldSupportSimpleFactoryWhen1Injection()
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
                                   DateTimeOffset Time { get; }
                           
                                   bool IsInitialized { get; }
                               }
                           
                               class Dependency : IDependency
                               {
                                   public DateTimeOffset Time { get; private set; }
                           
                                   public bool IsInitialized { get; private set; }
                           
                                   public IDependency Initialize(DateTimeOffset time)
                                   {
                                       Time = time;
                                       IsInitialized = true;
                                       return this;
                                   }
                               }
                           
                               interface IService
                               {
                                   IDependency Dependency { get; }
                               }
                           
                               class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                       Dependency = dependency;
                                   }
                           
                                   public IDependency Dependency { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To(_ => DateTimeOffset.Now)
                                           .Bind().To((Dependency dependency) => dependency.Initialize(DateTimeOffset.Now))
                                           .Bind().To<Service>()
                                           .Root<IService>("MyService");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.MyService;
                                       Console.WriteLine(service.Dependency.IsInitialized);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSimpleFactoryWhen2Injections()
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
                                   DateTimeOffset Time { get; }
                           
                                   bool IsInitialized { get; }
                               }
                           
                               class Dependency : IDependency
                               {
                                   public DateTimeOffset Time { get; private set; }
                           
                                   public bool IsInitialized { get; private set; }
                           
                                   public IDependency Initialize(DateTimeOffset time)
                                   {
                                       Time = time;
                                       IsInitialized = true;
                                       return this;
                                   }
                               }
                           
                               interface IService
                               {
                                   IDependency Dependency { get; }
                               }
                           
                               class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                       Dependency = dependency;
                                   }
                           
                                   public IDependency Dependency { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To(_ => DateTimeOffset.Now)
                                           .Bind().To((Dependency dependency, DateTimeOffset time) => dependency.Initialize(time))
                                           .Bind().To<Service>()
                                           .Root<IService>("MyService");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.MyService;
                                       Console.WriteLine(service.Dependency.IsInitialized);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSimpleFactoryWhenAutoBinding()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency {}
                           
                               interface IService
                               {
                                   IDependency? Dep { get; }
                                   
                                   IService Initialize(IDependency dep);
                               }
                           
                               class Service: IService 
                               {
                                   public IDependency? Dep { get; private set; }
                                   
                                   public IService Initialize(IDependency dep)
                                   {
                                       Dep = dep;
                                       return this;
                                   }
                                   
                                   public override string ToString()
                                   {
                                       return Dep?.ToString() ?? "";
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Service>()
                                           .Bind().To<IService, Dependency, string>((service, dependency) => service.Initialize(dependency).ToString() ?? "")
                                           .Root<string>("DepName");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var depName = composition.DepName;
                                       Console.WriteLine(depName);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Dependency"], result);
    }

    [Fact]
    public async Task ShouldSupportSimpleFactoryWhenInjectionWithOutTypeInLambda()
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
                                   DateTimeOffset Time { get; }
                           
                                   bool IsInitialized { get; }
                               }
                           
                               class Dependency : IDependency
                               {
                                   public DateTimeOffset Time { get; private set; }
                           
                                   public bool IsInitialized { get; private set; }
                           
                                   public IDependency Initialize(DateTimeOffset time)
                                   {
                                       Time = time;
                                       IsInitialized = true;
                                       return this;
                                   }
                               }
                           
                               interface IService
                               {
                                   IDependency Dependency { get; }
                               }
                           
                               class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                       Dependency = dependency;
                                   }
                           
                                   public IDependency Dependency { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To(_ => DateTimeOffset.Now)
                                           .Bind().To<Dependency, IDependency>(dependency => dependency.Initialize(DateTimeOffset.Now))
                                           .Bind().To<Service>()
                                           .Root<IService>("MyService");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.MyService;
                                       Console.WriteLine(service.Dependency.IsInitialized);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSimpleFactoryWhenSimpleLambda()
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
                                   DateTimeOffset Time { get; }
                           
                                   bool IsInitialized { get; }
                               }
                           
                               class Dependency : IDependency
                               {
                                   public DateTimeOffset Time { get; private set; }
                           
                                   public bool IsInitialized { get; private set; }
                           
                                   public IDependency Initialize(DateTimeOffset time)
                                   {
                                       Time = time;
                                       IsInitialized = true;
                                       return this;
                                   }
                               }
                           
                               interface IService
                               {
                                   IDependency Dependency { get; }
                               }
                           
                               class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                       Dependency = dependency;
                                   }
                           
                                   public IDependency Dependency { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind("now").To(_ => DateTimeOffset.Now)
                                           .Bind().To((Dependency dependency, [Tag("now")] DateTimeOffset time) => 
                                               {
                                                   return dependency.Initialize(time);
                                               })
                                           .Bind().To<Service>()
                                           .Root<IService>("MyService");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.MyService;
                                       Console.WriteLine(service.Dependency.IsInitialized);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSimpleFactoryWhenSimpleLambdaWithGenericParams()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency {}
                           
                               interface IService
                               {
                                   IDependency? Dep { get; }
                                   
                                   IService Initialize(IDependency dep);
                               }
                           
                               class Service: IService 
                               {
                                   public IDependency? Dep { get; private set; }
                                   
                                   public IService Initialize(IDependency dep)
                                   {
                                       Dep = dep;
                                       return this;
                                   }
                                   
                                   public override string ToString()
                                   {
                                       return Dep?.ToString() ?? "";
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind().To<Service>()
                                           .Bind<string>().To<IService, IDependency, string>((service, dependency) =>
                                               { 
                                                   return service.Initialize(dependency).ToString() ?? "";
                                               })
                                           .Root<string>("DepName");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var depName = composition.DepName;
                                       Console.WriteLine(depName);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Dependency"], result);
    }

    [Fact]
    public async Task ShouldSupportSimpleFactoryWhenTag()
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
                                   DateTimeOffset Time { get; }
                           
                                   bool IsInitialized { get; }
                               }
                           
                               class Dependency : IDependency
                               {
                                   public DateTimeOffset Time { get; private set; }
                           
                                   public bool IsInitialized { get; private set; }
                           
                                   public IDependency Initialize(DateTimeOffset time)
                                   {
                                       Time = time;
                                       IsInitialized = true;
                                       return this;
                                   }
                               }
                           
                               interface IService
                               {
                                   IDependency Dependency { get; }
                               }
                           
                               class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                       Dependency = dependency;
                                   }
                           
                                   public IDependency Dependency { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind("now").To(_ => DateTimeOffset.Now)
                                           .Bind().To((Dependency dependency, [Tag("now")] DateTimeOffset time) => dependency.Initialize(time))
                                           .Bind().To<Service>()
                                           .Root<IService>("MyService");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.MyService;
                                       Console.WriteLine(service.Dependency.IsInitialized);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSimpleFactoryWhenTypeAreInLambda()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency {}
                           
                               interface IService
                               {
                                   IDependency? Dep { get; }
                                   
                                   IService Initialize(IDependency dep);
                               }
                           
                               class Service: IService 
                               {
                                   public IDependency? Dep { get; private set; }
                                   
                                   public IService Initialize(IDependency dep)
                                   {
                                       Dep = dep;
                                       return this;
                                   }
                                   
                                   public override string ToString()
                                   {
                                       return Dep?.ToString() ?? "";
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind().To<Service>()
                                           .Bind().To((IService service, IDependency dependency) => service.Initialize(dependency).ToString() ?? "")
                                           .Root<string>("DepName");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var depName = composition.DepName;
                                       Console.WriteLine(depName);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Dependency"], result);
    }
}