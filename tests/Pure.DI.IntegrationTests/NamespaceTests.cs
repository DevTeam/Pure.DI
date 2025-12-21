namespace Pure.DI.IntegrationTests;

using static Path;

/// <summary>
/// Tests related to the namespace handling in the generated code.
/// </summary>
public class NamespaceTests
{
    [Fact]
    public async Task ShouldSupportNamespaceForFuncWithGenericParams()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using System.Text.RegularExpressions;

                           namespace Sample
                           {
                               interface IService
                               {     
                               }
                           
                               class Service: IService 
                               {
                                   public Service(Func<string, Regex> regFactory)
                                   {             
                                       Console.WriteLine(regFactory(".+"));
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Func<string, Regex>>().To(_ => new Func<string, Regex>(value => new Regex(value, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase)))
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service =  composition.Service;                               
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([".+"], result);
    }

    [Fact]
    public async Task ShouldSupportNamespaceFromExtensionMethod()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace OtherNamespace
                           {
                               static class Utils
                               {
                                   public static Sample.IDependency DoSomething(this Sample.IDependency dep) => dep;
                               }
                           }

                           namespace Sample
                           {
                               using OtherNamespace;
                           
                               interface IDependency {}
                           
                               class Dependency: IDependency {}
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency dep)
                                   { 
                                       Dep = dep;
                                   }
                           
                                   public IDependency Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<IDependency>(_ => new Dependency().DoSomething())
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition(); 
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }
    [Fact]
    public async Task ShouldSupportNamespaceInCompositionName()
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
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency dep)
                                   { 
                                       Dep = dep;
                                   }
                           
                                   public IDependency Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("MyNs.Abc.Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new MyNs.Abc.Composition();
                                       Console.WriteLine(composition.GetType());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["MyNs.Abc.Composition"], result);
    }

    [Fact]
    public async Task ShouldUseDefaultFileScopedNamespaceDeclaration()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace My.Sample;

                           interface IDependency {}

                           class Dependency: IDependency {}

                           interface IService
                           {
                               IDependency Dep { get; }
                           }

                           class Service: IService 
                           {
                               public Service(IDependency dep)
                               { 
                                   Dep = dep;
                               }
                           
                               public IDependency Dep { get; }
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().To<Dependency>()
                                       .Bind<IService>().To<Service>()
                                       .Root<IService>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new My.Sample.Composition();
                                   Console.WriteLine(composition.GetType());
                               }
                           }

                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["My.Sample.Composition"], result);
    }

    [Fact]
    public async Task ShouldUseDefaultNamespaceDeclaration()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace My.Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency {}
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency dep)
                                   { 
                                       Dep = dep;
                                   }
                           
                                   public IDependency Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new My.Sample.Composition();
                                       Console.WriteLine(composition.GetType());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["My.Sample.Composition"], result);
    }

    [Fact]
    public async Task ShouldSupportStaticUsing()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.IO;
                           using Pure.DI;
                           using static System.IO.Path;
                           using static System.DateTime;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(DateTime now)
                                   {
                                   }
                               }
                           
                               interface IService
                               {
                                   IDependency? Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] char pathSeparator)
                                   {
                                       Console.WriteLine($"Initialize 1 {pathSeparator}");
                                   }
                           
                                   [Ordinal(0)]
                                   public IDependency? Dep { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => PathSeparator)
                                           .Bind().To(_ => Now)
                                           .Bind().To<Dependency>()
                                           .Root<Service>();
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Resolve<Service>();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([$"Initialize 1 {PathSeparator}"], result);
    }

    [Fact]
    public async Task ShouldSupportUsingAliases()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.IO;
                           using Pure.DI;
                           using static System.IO.Path;
                           using MyDataTime = System.DateTime;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency
                               {
                                   public Dependency(DateTime now)
                                   {
                                   }
                               }
                           
                               interface IService
                               {
                                   IDependency? Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] char pathSeparator)
                                   {
                                       Console.WriteLine($"Initialize 1 {pathSeparator}");
                                   }
                           
                                   [Ordinal(0)]
                                   public IDependency? Dep { get; set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind(374).To(_ => PathSeparator)
                                           .Bind().To(_ => MyDataTime.Now)
                                           .Bind().To<Dependency>()
                                           .Root<Service>();
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Resolve<Service>();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([$"Initialize 1 {PathSeparator}"], result);
    }
}