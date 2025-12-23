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
    [Fact]
    public async Task ShouldSupportGlobalNamespace()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           interface IDependency {}
                           class Dependency: IDependency {}

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IDependency>().To<Dependency>()
                                       .Root<IDependency>("Root");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Root != null);
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportDeepNestedNamespaces()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace A.B.C.D.E
                           {
                               interface IDependency {}
                               class Dependency: IDependency {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Root<IDependency>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new A.B.C.D.E.Composition();
                                       Console.WriteLine(composition.Root != null);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportGlobalUsing()
    {
        // Given

        // When
        var result = await """
                           global using System;
                           global using Pure.DI;
                           using System.Collections.Generic;

                           namespace Sample
                           {
                               interface IDependency {}
                               class Dependency: IDependency {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Root<IDependency>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root != null);
                                   }
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportGlobalUsingAlias()
    {
        // Given

        // When
        var result = await """
                           global using MyInt = System.Int32;
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                               class Dependency: IDependency 
                               {
                                   public Dependency(MyInt value) {}
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<MyInt>().To(_ => 10)
                                           .Bind<IDependency>().To<Dependency>()
                                           .Root<IDependency>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root != null);
                                   }
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportTypesInGlobalNamespaceWithGlobalPrefix()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           interface IDependency {}
                           class Dependency: IDependency {}

                           namespace Sample
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<global::IDependency>().To<global::Dependency>()
                                           .Root<global::IDependency>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root != null);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSameClassNameInDifferentNamespaces()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Ns1
                           {
                               public class Service {}
                           }

                           namespace Ns2
                           {
                               public class Service {}
                           }

                           namespace Sample
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Ns1.Service>().To<Ns1.Service>()
                                           .Bind<Ns2.Service>().To<Ns2.Service>()
                                           .Root<Ns1.Service>("Root1")
                                           .Root<Ns2.Service>("Root2");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root1.GetType().Namespace);
                                       Console.WriteLine(composition.Root2.GetType().Namespace);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Ns1", "Ns2"], result);
    }

    [Fact]
    public async Task ShouldSupportNamespaceWithDotsInCompositionName()
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
                                       DI.Setup("My.Company.MyComposition")
                                           .Bind<int>().To(_ => 10)
                                           .Root<int>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new My.Company.MyComposition();
                                       Console.WriteLine(composition.GetType().FullName);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["My.Company.MyComposition"], result);
    }

    [Fact]
    public async Task ShouldSupportNamespaceInBinding()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Ns1
                           {
                               public interface IService {}
                               internal class Service : IService {}
                           }

                           namespace Sample
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Ns1.IService>().To<Ns1.Service>()
                                           .Root<Ns1.IService>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Namespace);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Ns1"], result);
    }

    [Fact]
    public async Task ShouldSupportAliasedNamespace()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using Alias1 = Ns1;

                           namespace Ns1
                           {
                               public class Service {}
                           }

                           namespace Sample
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Alias1.Service>().To<Alias1.Service>()
                                           .Root<Alias1.Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Namespace);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Ns1"], result);
    }

    [Fact]
    public async Task ShouldSupportNestedClassInNamespace()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Ns1
                           {
                               public class Outer
                               {
                                   public class Inner {}
                               }
                           }

                           namespace Sample
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Ns1.Outer.Inner>().To<Ns1.Outer.Inner>()
                                           .Root<Ns1.Outer.Inner>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().FullName);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Ns1.Outer+Inner"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericTypeFromOtherNamespace()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using System.Collections.Generic;

                           namespace Ns1
                           {
                               public class Service<T> {}
                           }

                           namespace Sample
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Ns1.Service<int>>().To<Ns1.Service<int>>()
                                           .Root<Ns1.Service<int>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Namespace);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Ns1"], result);
    }

    [Fact]
    public async Task ShouldSupportMultipleNamespacesInSameFile()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Ns1
                           {
                               public class Service1 {}
                           }

                           namespace Ns2
                           {
                               public class Service2 {}
                           }

                           namespace SetupNs
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Ns1.Service1>().To<Ns1.Service1>()
                                           .Bind<Ns2.Service2>().To<Ns2.Service2>()
                                           .Root<Ns1.Service1>("Root1")
                                           .Root<Ns2.Service2>("Root2");
                                   }
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new SetupNs.Composition();
                                   Console.WriteLine(composition.Root1.GetType().Namespace);
                                   Console.WriteLine(composition.Root2.GetType().Namespace);
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Ns1", "Ns2"], result);
    }

    [Fact]
    public async Task ShouldSupportNamespaceWithVerbatimIdentifier()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace @class
                           {
                               public class Service {}
                           }

                           namespace Sample
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<@class.Service>().To<@class.Service>()
                                           .Root<@class.Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Namespace);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["class"], result);
    }

    [Fact]
    public async Task ShouldSupportNamespaceWithGlobalAlias()
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
                                           .Bind<global::System.IDisposable>().To(_ => new global::System.IO.MemoryStream())
                                           .Root<global::System.IDisposable>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().FullName);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["System.IO.MemoryStream"], result);
    }

    [Fact]
    public async Task ShouldSupportUsingInsideNamespace()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Ns1
                           {
                               public class Service {}
                           }

                           namespace Sample
                           {
                               using Ns1;
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
                                       Console.WriteLine(composition.Root.GetType().Namespace);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Ns1"], result);
    }

    [Fact]
    public async Task ShouldSupportStaticUsingInsideNamespace()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Ns1
                           {
                               public static class Utils
                               {
                                   public static int Val = 10;
                               }
                           }

                           namespace Sample
                           {
                               using static Ns1.Utils;
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<int>().To(_ => Val)
                                           .Root<int>("Root");
                                   }
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
        result.StdOut.ShouldBe(["10"], result);
    }

    [Fact]
    public async Task ShouldSupportNamespaceAliasInsideNamespace()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Ns1
                           {
                               public class Service {}
                           }

                           namespace Sample
                           {
                               using MyNs = Ns1;
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<MyNs.Service>().To<MyNs.Service>()
                                           .Root<MyNs.Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Namespace);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Ns1"], result);
    }

    [Fact]
    public async Task ShouldSupportNestedNamespaceInFileScopedNamespace()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace My.Sample;

                           public class Service {}

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
                                   var composition = new My.Sample.Composition();
                                   Console.WriteLine(composition.Root.GetType().Namespace);
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp10 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["My.Sample"], result);
    }

    [Fact]
    public async Task ShouldSupportNamespaceForRecordType()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Ns1
                           {
                               public record Service(int Id);
                           }

                           namespace Sample
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Ns1.Service>().To(_ => new Ns1.Service(1))
                                           .Root<Ns1.Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Namespace);
                                   }
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp9 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Ns1"], result);
    }

    [Fact]
    public async Task ShouldSupportNamespaceWithDotsInGenericArgument()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using System.Collections.Generic;

                           namespace My.Ns
                           {
                               public class Data {}
                           }

                           namespace Sample
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<List<My.Ns.Data>>().To(_ => new List<My.Ns.Data>())
                                           .Root<List<My.Ns.Data>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().GenericTypeArguments[0].Namespace);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["My.Ns"], result);
    }

    [Fact]
    public async Task ShouldSupportNamespaceWithMultipleUsingDirectives()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;

                           namespace Ns1
                           {
                               public class Service {}
                           }

                           namespace Sample
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Ns1.Service>().To<Ns1.Service>()
                                           .Root<Ns1.Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Namespace);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Ns1"], result);
    }

    [Fact]
    public async Task ShouldSupportNamespaceForTuple()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Ns1
                           {
                               public class Service {}
                           }

                           namespace Sample
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<(Ns1.Service, int)>().To(_ => (new Ns1.Service(), 1))
                                           .Root<(Ns1.Service, int)>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.Item1.GetType().Namespace);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Ns1"], result);
    }

    [Fact]
    public async Task ShouldSupportNamespaceForGenericStruct()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Ns1
                           {
                               public struct Service<T> {}
                           }

                           namespace Sample
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Ns1.Service<string>>().To<Ns1.Service<string>>()
                                           .Root<Ns1.Service<string>>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().Namespace);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Ns1"], result);
    }

    [Fact]
    public async Task ShouldSupportGlobalNamespaceForComposition()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Ns1
                           {
                               public class Service {}
                           }

                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Ns2.Composition")
                                       .Bind<Ns1.Service>().To<Ns1.Service>()
                                       .Root<Ns1.Service>("Root");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Ns2.Composition();
                                   Console.WriteLine(composition.Root.GetType().Namespace);
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Ns1"], result);
    }

    [Fact]
    public async Task ShouldSupportNamespaceForDelegate()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Ns1
                           {
                               public delegate string MyDelegate(int value);
                           }

                           namespace Sample
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Ns1.MyDelegate>().To(_ => new Ns1.MyDelegate(v => v.ToString()))
                                           .Root<Ns1.MyDelegate>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root(10));
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["10"], result);
    }

    [Fact]
    public async Task ShouldSupportNamespaceForArray()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Ns1
                           {
                               public class Service {}
                           }

                           namespace Sample
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<Ns1.Service[]>().To(_ => new [] { new Ns1.Service() })
                                           .Root<Ns1.Service[]>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root.GetType().GetElementType()?.Namespace);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Ns1"], result);
    }
}