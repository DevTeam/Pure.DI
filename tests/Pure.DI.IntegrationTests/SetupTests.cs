namespace Pure.DI.IntegrationTests;

using System.Text;
using Core;

/// <summary>
/// Tests related to the general DI setup and configuration.
/// </summary>
public class SetupTests
{
    [Fact]
    public async Task ShouldCreateCompositionRootWhenSomeOtherCompositionHasInvalidGraph()
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
                                       DI.Setup("InvalidComposition")
                                           .Bind<string>().To(_ => "Abc")
                                           .Root<string>("Root1")
                                           .Root<string>("Root2");
                                           
                                       DI.Setup("Composition")
                                           .Bind<string>().To(_ => "Abc")
                                           .Root<string>("Root");
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
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ShouldCreateCompositionRootWhenSomeOtherCompositionHasInvalidMetadata()
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
                                       DI.Setup("InvalidComposition")
                                           .Bind<string>().To(_ => 33)
                                           .Root<string>("Root");
                                           
                                       DI.Setup("Composition")
                                           .Bind<string>().To(_ => "Abc")
                                           .Root<string>("Root");
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
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(1);
    }
    [Fact]
    public async Task ShouldGenerateCompositionWhenHasNoAnyRoots()
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
                                           .Bind<string>().To(_ => "Abc");
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
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(2, result);
        result.Warnings.Count(i => i.Id == LogId.WarningNoRoots && i.Locations.FirstOrDefault().GetSource() == "Setup(\"Composition\")").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldSupportSetupInvocationViaAlias()
    {
        // Given

        // When
        var result = await """
                           using Pure.DI;
                           using MyDI = Pure.DI.DI;

                           namespace Sample
                           {
                               interface IDependency { }

                               class Dependency : IDependency { }

                               class Service
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       MyDI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportSetupInvocationViaGlobalQualifier()
    {
        // Given

        // When
        var result = await """
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency { }

                               class Dependency : IDependency { }

                               class Service
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       global::Pure.DI.DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldGenerateSpecifiedRootType()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                           internal interface IDependency { }
                           
                               internal class Dependency : IDependency { }
                           
                               internal interface IService1 { }
                           
                               internal interface IService2 { }
                           
                               internal interface IService3 { }
                           
                               internal class Service : IService1, IService2, IService3
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService1>().Bind<IService2>().Bind<IService3>().To<Service>()
                                           .Root<IService2>("Service");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();  
                                       IService2 service2 = composition.Service;                           
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldNotOverrideBindingForDifferentMarkerTypes()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                            .Bind<Func<char, IList<TT1>>>().To<Func<char, IList<TT1>>>(_ => ch => new List<TT1>())
                                            .Bind<Func<byte, TT1[]>>().To<Func<byte, TT1[]>>(_ => ch => new TT1[0])
                                            .Bind<Func<int, IList<TT2>>>().To<Func<int, IList<TT2>>>(_ => ch => new List<TT2>())
                                            .Root<Func<char, IList<string>>>("Root1")
                                            .Root<Func<byte, int[]>>("Root2")
                                            .Root<Func<int, IList<char>>>("Root3");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                        var composition = new Composition();
                                        Console.WriteLine(composition.Root1.GetType());
                                        Console.WriteLine(composition.Root2.GetType());
                                        Console.WriteLine(composition.Root3.GetType());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(
            [
                "System.Func`2[System.Char,System.Collections.Generic.IList`1[System.String]]",
                "System.Func`2[System.Byte,System.Int32[]]",
                "System.Func`2[System.Int32,System.Collections.Generic.IList`1[System.Char]]"
            ],
            result);
        result.Warnings.Count.ShouldBe(0, result);
        result.Warnings.Count(i => i.Id == LogId.WarningOverriddenBinding && i.Locations.FirstOrDefault().GetSource() == "").ShouldBe(0, result);
    }

    [Fact]
    public async Task ShouldOverrideBinding()
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
                                           .Bind<string>().To(_ => "Abc")
                                           .Bind<string>().To(_ => "Xyz")
                                           .Root<string>("Result");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Result);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.StdOut.ShouldBe(["Xyz"], result);
        result.Warnings.Count.ShouldBe(2, result);
        result.Warnings.Count(i => i.Id == LogId.WarningOverriddenBinding && i.Locations.FirstOrDefault().GetSource() == "To(_ => \"Xyz\")").ShouldBe(1, result);
        result.Warnings.Count(i => i.Id == LogId.WarningBindingNotUsed && i.Locations.FirstOrDefault().GetSource() == "To(_ => \"Abc\")").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldSupportCultureForErrorsAndWarnings()
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
                                           .Bind<string>().To(_ => "Abc")
                                           .Bind<string>().To(_ => "Xyz")
                                           .Root<string>("Result");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Result);
                                   }
                               }
                           }
                           """.RunAsync(new Options( Culture: "ru" ));

        // Then
        result.Success.ShouldBeFalse(result);
        result.StdOut.ShouldBe(["Xyz"], result);
        result.Warnings.Count.ShouldBe(2, result);
        result.Warnings.Count(i => i is { Id: LogId.WarningOverriddenBinding, Message: "Привязка для string была переопределена." } && i.Locations.FirstOrDefault().GetSource() == "To(_ => \"Xyz\")").ShouldBe(1, result);
        result.Warnings.Count(i => i is { Id: LogId.WarningBindingNotUsed, Message: "Привязка не используется." } && i.Locations.FirstOrDefault().GetSource() == "To(_ => \"Abc\")").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldOverrideBindingWhenDependency()
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
                                   string Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(string dep) => Dep = dep;
                           
                                   public string Dep { get; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupBaseComposition()
                                   {
                                       DI.Setup("BaseComposition", CompositionKind.Internal)
                                           .Bind<IService>().To<Service>()
                                           .Bind<string>().To(_ => "Abc");
                                   }
                           
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition").DependsOn("BaseComposition")
                                           .Bind<string>().To(_ => "Xyz")
                                           .Root<IService>("Result");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Result.Dep);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.StdOut.ShouldBe(["Xyz"], result);
        result.Warnings.Count.ShouldBe(1, result);
        result.Warnings.Count(i => i.Id == LogId.WarningOverriddenBinding && i.Locations.FirstOrDefault().GetSource() == "To(_ => \"Xyz\")").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldOverrideBindingWhenDependsOn()
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
                                   private static void SetupBaseComposition()
                                   {
                                       DI.Setup("BaseComposition")
                                           .Root<string>("Result")
                                           .Bind<string>().To(_ => "Abc");
                                   }
                           
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition").DependsOn("BaseComposition")
                                           .Bind<string>().To(_ => "Xyz");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Result);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.StdOut.ShouldBe(["Xyz"], result);
        result.Warnings.Count.ShouldBe(2, result);
        result.Warnings.Count(i => i.Id == LogId.WarningOverriddenBinding && i.Locations.FirstOrDefault().GetSource() == "To(_ => \"Xyz\")").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldOverrideBindingWhenGlobal()
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
                                   private static void SetupGlobalComposition()
                                   {
                                       DI.Setup("", CompositionKind.Global)
                                           .Root<string>("Result")
                                           .Bind<string>().To(_ => "Abc");
                                   }
                           
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<string>().To(_ => "Xyz");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Result);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Xyz"], result);
    }

    [Fact]
    public async Task ShouldOverrideBindingWithSeveralInterfaces()
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
                                   void Do();
                               }
                               
                               interface IAbc
                               {
                               }
                               
                               interface IXyz
                               {
                               }
                               
                               class Dependency : IDependency, IAbc
                               {
                                   public void Do()
                                   {
                                   }
                               }
                               
                               class MyDependency : IDependency, IXyz
                               {
                                   public void Do()
                                   {
                                   }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Dependency>()
                                           .Bind().To(_ => new MyDependency())
                                           .Root<IDependency>("Dep")
                                           .Root<IAbc>("Abc")
                                           .Root<IXyz>("Xyz");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Dep);
                                       Console.WriteLine(composition.Abc);
                                       Console.WriteLine(composition.Xyz);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.StdOut.ShouldBe(["Sample.MyDependency", "Sample.Dependency", "Sample.MyDependency"], result);
        result.Warnings.Count.ShouldBe(1);
        result.Warnings.Count(i => i.Id == LogId.WarningOverriddenBinding && i.Locations.FirstOrDefault().GetSource() == "To(_ => new MyDependency())").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldOverrideGlobalBindingWithoutWarning()
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
                                           .Bind<string>().To(_ => "Abc")
                                           .Bind<global::System.Collections.Generic.IComparer<TT>>().To(_ => global::System.Collections.Generic.Comparer<TT>.Default)            
                                           .Root<string>("Result");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Result);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(1, result);
        result.Warnings.Count(i => i.Id == LogId.WarningBindingNotUsed && i.Locations.FirstOrDefault().GetSource() == "To(_ => global::System.Collections.Generic.Comparer<TT>.Default)").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldShowErrorWhenDuplicatedRoots()
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
                                           .Bind<string>().To(_ => "Abc")
                                           .Root<string>("Root1")
                                           .Root<string>("Root2")
                                           .Root<string>("Root3");
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
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(2, result);
    }

    [Fact]
    public async Task ShouldSupportBindWhenHasNoTypeParams()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                           
                               internal interface IService { }
                           
                               internal class Service: IService { }
                               
                               internal partial class Composition
                               {                   
                                   void Setup() => 
                                       DI.Setup("Composition")
                                           .Bind(typeof(Service)).To<Service>()
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

    [Theory]
    [InlineData("System.IDisposable")]
    [InlineData("UnityEngine.MonoBehaviour")]
    [InlineData("UnityEngine.ScriptableObject")]
    [InlineData("UnityEngine.Object")]
    [InlineData("IComparer<int>")]
    public async Task ShouldSupportBindWhenHasNoTypeParamsAndSpecialType(string baseType)
    {
        // Given

        // When
        var result = await """
                           namespace UnityEngine
                           {
                                public interface Object {}
                                public abstract class ScriptableObject: Object {}
                                public class MonoBehaviour: Object {}
                           }
                           
                           namespace Sample
                           {
                               using System;
                               using System.Collections.Generic;
                               using Pure.DI;
                               using Sample;
                           
                               internal class Dep1: #baseType#
                               {
                                   public void Dispose() { }
                                   
                                   public int Compare(int x, int y) => 0;
                               }
                               
                               internal class Dep2: #baseType#
                               {
                                   public void Dispose() { }
                                   
                                   public int Compare(int x, int y) => 0;
                               }
                               
                               internal partial class Composition
                               {                   
                                   void Setup() => 
                                       DI.Setup("Composition")
                                           .SpecialType<UnityEngine.Object>()
                                           .SpecialType<UnityEngine.ScriptableObject>()
                                           .Bind().To<Dep1>()
                                           .Bind().To<Dep2>()
                                           .Root<Dep1>("Root1")
                                           .Root<Dep2>("Root2")
                                           .SpecialType<UnityEngine.MonoBehaviour>()
                                           .SpecialType<IComparer<int>>(); 
                               }       
                           
                               public class Program
                               {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                  }
                               }
                           }
                           """.Replace("#baseType#", baseType).RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

#if ROSLYN4_8_OR_GREATER
    [Fact]
    public async Task ShouldSupportDependentCompositionsWhenInterface()
    {
        // Given

        // When
        var result = await """
                           using Pure.DI;
                           #pragma warning disable CS9113 // Parameter is unread.
                           
                           namespace Sample
                           {
                               interface IInfrastructure
                               {
                                   void Setup() => DI.Setup(kind: CompositionKind.Internal)
                                       .Bind<IDatabase>().To<SqlDatabase>();
                               }
                               
                               partial class Composition: IInfrastructure
                               {
                                   void Setup() => DI.Setup(kind: CompositionKind.Internal)
                                       .Bind<IUserService>().To<UserService>()
                                       .Root<IUserService>("UserService");
                               }
                               
                               partial class OtherComposition: Composition
                               {
                                   void Setup() => DI.Setup()
                                       .Root<Ui>("Ui");
                               }
                               
                               interface IDatabase;
                           
                               class SqlDatabase : IDatabase;
                               
                               interface IUserService;
                               
                               class UserService(IDatabase database) : IUserService;
                               
                               partial class Ui(IUserService userService)
                               {
                                   public IUserService UserService { get; } = userService;
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var otherComposition = new OtherComposition();
                                       var userService = otherComposition.Ui.UserService;
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp12));

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportDependentCompositionsWhenMultBaseTypes()
    {
        // Given

        // When
        var result = await """
                           using Pure.DI;
                           #pragma warning disable CS9113 // Parameter is unread.
                           
                           namespace Sample
                           {
                               interface IInfrastructure
                               {
                                   void Setup() => DI.Setup(kind: CompositionKind.Internal)
                                       .Bind<IDatabase>().To<SqlDatabase>();
                               }
                               
                               partial class Composition: IInfrastructure
                               {
                                   void Setup() => DI.Setup(kind: CompositionKind.Internal)
                                       .Bind<IUserService>().To<UserService>()
                                       .Root<IUserService>("UserService");
                               }
                               
                               partial class OtherComposition: Composition
                               {
                                   void Setup() => DI.Setup()
                                       .Root<Ui>("Ui");
                               }
                               
                               interface IDatabase;
                           
                               class SqlDatabase : IDatabase;
                               
                               interface IUserService;
                               
                               class UserService(IDatabase database) : IUserService;
                               
                               partial class Ui(IUserService userService)
                               {
                                   public IUserService UserService { get; } = userService;
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var otherComposition = new OtherComposition();
                                       var userService = otherComposition.Ui.UserService;
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp12));

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportDeepInheritanceOfCompositions()
    {
        // Given

        // When
        var result = await """
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               interface IBase1 { void Setup1() => DI.Setup(kind: CompositionKind.Internal).Bind<string>().To(_ => "Base1"); }
                               interface IBase2 : IBase1 { void Setup2() => DI.Setup(kind: CompositionKind.Internal).Bind<int>().To(_ => 1); }
                               abstract class BaseClass : IBase2 { void Setup3() => DI.Setup(kind: CompositionKind.Internal).Bind<double>().To(_ => 2.0); }
                               
                               partial class Composition : BaseClass
                               {
                                   void Setup() => DI.Setup()
                                       .Root<(string, int, double)>("Root");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp12));

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportMultipleBaseInterfacesForComposition()
    {
        // Given

        // When
        var result = await """
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               interface IBase1 { void Setup1() => DI.Setup(kind: CompositionKind.Internal).Bind<string>().To(_ => "Base1"); }
                               interface IBase2 { void Setup2() => DI.Setup(kind: CompositionKind.Internal).Bind<int>().To(_ => 1); }
                               
                               partial class Composition : IBase1, IBase2
                               {
                                   void Setup() => DI.Setup()
                                       .Root<(string, int)>("Root");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp12));

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportInheritanceWithSkippedSetup()
    {
        // Given

        // When
        var result = await """
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               interface IBase { void SetupBase() => DI.Setup(kind: CompositionKind.Internal).Bind<string>().To(_ => "Base"); }
                               
                               abstract class MiddleClass : IBase { }
                               
                               partial class Composition : MiddleClass
                               {
                                   void Setup() => DI.Setup()
                                       .Root<string>("Root");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp12));

        // Then
        result.Success.ShouldBeTrue(result);
    }
#endif

    [Fact]
    public async Task ShouldSupportBindWhenHasNoTypeParamsAndEnums()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                           
                               internal enum MyEnum1
                               {
                                   Abc, Xyz
                               }
                               
                               internal enum MyEnum2
                               {
                                   Asd, Fgh
                               }
                               
                               internal partial class Composition
                               {                   
                                   void Setup() => 
                                       DI.Setup("Composition")
                                           .Bind().To<MyEnum1>()
                                           .Bind().To<MyEnum2>()
                                           .Root<MyEnum1>("Root1")
                                           .Root<MyEnum2>("Root2"); 
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
        result.StdOut.ShouldBe(["Abc", "Asd"], result);
    }

    [Fact]
    public async Task ShouldSupportCombinedSetup()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal interface IDependency { }
                           
                               internal class Dependency : IDependency { }
                           
                               internal interface IService { }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IDependency>(1).To<Dependency>()
                                           .Bind<IDependency>(2).To<Dependency>()
                                           .Bind<IDependency>(3).To<Dependency>()
                                           .Bind<IDependency>(4).To<Dependency>()
                                           .Bind<IDependency>(5).To<Dependency>()
                                           .Bind<IDependency>(6).To<Dependency>()
                                           .Bind<IDependency>(7).To<Dependency>();
                           
                                       DI.Setup("Composition")
                                           .Bind<IService>().To<Service>().Root<IService>();
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
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(7, result);
        result.Warnings.Count(i => i.Id == LogId.WarningBindingNotUsed && i.Locations.FirstOrDefault().GetSource() == "To<Dependency>()").ShouldBe(7, result);
    }

    [Fact]
    public async Task ShouldSupportDefaultCompositionName()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal interface IDependency { }
                           
                               internal class Dependency : IDependency { }
                           
                               internal interface IService { }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }
                           
                               partial class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup()
                                           .Bind<IDependency>().To<Dependency>();
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       DI.Setup("Composition")
                                           .DependsOn("Sample.Setup")
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Root");
                           
                                       var composition = new Composition();
                                       System.Console.WriteLine(composition.Root); 
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Errors.ShouldBeEmpty();
        result.StdOut.ShouldBe(["Sample.Service"], result);
    }

    [Fact]
    public async Task ShouldSupportFewBindings()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                           internal interface IDependency { }
                           
                               internal class Dependency : IDependency { }
                           
                               internal interface IService1 { }
                           
                               internal interface IService2 { }
                           
                               internal interface IService3 { }
                           
                               internal class Service : IService1, IService2, IService3
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService1, IService2, IService3>().To<Service>()
                                           .Root<IService2>("Service2")
                                           .Root<IService3>("Service3");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       IService2 service2 = composition.Service2;
                                       IService3 service3 = composition.Service3;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportFewRoots()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                           internal interface IDependency { }
                           
                               internal class Dependency : IDependency { }
                           
                               internal interface IService1 { }
                           
                               internal interface IService2 { }
                           
                               internal interface IService3 { }
                           
                               internal class Service : IService1, IService2, IService3
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IService1>().Bind<IService2>().Bind<IService3>().To<Service>()
                                           .Root<IService2>("Service2")
                                           .Root<IService3>("Service3");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();  
                                       IService2 service2 = composition.Service2;
                                       IService3 service3 = composition.Service3;                           
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportInjectWhenNamedParams()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                           
                               internal interface IService { }
                           
                               internal class Service: IService { }
                               
                               internal class Consumer
                               {
                                   public Consumer(IService service)
                                   {
                                   }
                               }
                               
                               internal partial class Composition
                               {                   
                                   void Setup() => 
                                       DI.Setup("Composition")
                                       .Bind<IService>("Abc").To<Service>()
                                       .Bind<Consumer>().To(ctx => 
                                       {
                                           ctx.Inject(value: out IService service, tag: "Abc");
                                           return new Consumer(service);
                                       })
                                       .Root<Consumer>("Root"); 
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
        result.StdOut.ShouldBe(["Sample.Consumer"], result);
    }

    [Fact]
    public async Task ShouldSupportMethodRoot()
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
                                           .Bind<string>().To(_ => "Abc")
                                           .Root<string>("Root", default, RootKinds.Method);
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       Console.WriteLine(new Composition().Root());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportDependsOnBaseClass()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace RootNamespace
                           {
                           namespace Sample
                           {
                               class SetupBase
                               {
                                   private static void SetupBaseComposition()
                                   {
                                       DI.Setup("RootNamespace.Sample.SetupBase", CompositionKind.Internal)
                                           .Bind<int>().To(_ => 1);
                                   }
                               }
                           
                               partial class Setup: SetupBase
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Root<int>("Root");
                                   }
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new global::Sample.Composition();
                                       Console.WriteLine(composition.Root);
                                   }
                               }
                           }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1"], result);
    }

    [Fact]
    public async Task ShouldSupportDependsOnBaseClassWhenDefaultName()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class SetupBase
                               {
                                   private static void SetupBaseComposition()
                                   {
                                       DI.Setup(kind: CompositionKind.Internal)
                                           .Bind<int>().To(_ => 1);
                                   }
                               }

                               partial class Setup: SetupBase
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
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
        result.StdOut.ShouldBe(["1"], result);
    }

    [Fact]
    public async Task ShouldSupportInheritanceOfCompositions()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class SetupBase
                               {
                                   private static void SetupBaseComposition()
                                   {
                                       DI.Setup(kind: CompositionKind.Internal)
                                           .Bind<int>().To(_ => 1);
                                   }
                               }
                               
                               class SetupBase2: SetupBase
                               {
                                   private static void SetupBaseComposition()
                                   {
                                       DI.Setup(kind: CompositionKind.Internal)
                                           .Bind<string>().To(_ => "Abc");
                                   }
                               }

                               partial class Setup: SetupBase2
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Root<int>("Root")
                                           .Root<string>("Root2");
                                   }
                               }  

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root);
                                       Console.WriteLine(composition.Root2);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1", "Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportDependsOnBaseClassWhenDefaultNameAndHasNoNamespace()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           class SetupBase
                           {
                               private static void SetupBaseComposition()
                               {
                                   DI.Setup(kind: CompositionKind.Internal)
                                       .Bind<int>().To(_ => 1);
                               }
                           }

                           partial class Setup: SetupBase
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
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
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1"], result);
    }

#if ROSLYN4_8_OR_GREATER
    [Fact]
    public async Task ShouldSupportMultipleBaseCompositions()
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
                                   private static void SetupBaseComposition1()
                                   {
                                       DI.Setup("BaseComposition1")
                                           .RootBind<int>().To(_ => 1);
                                   }
                           
                                   private static void SetupBaseComposition2()
                                   {
                                       DI.Setup("BaseComposition2")
                                           .RootBind<string>().To(_ => "2");
                                   }
                           
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition").DependsOn(["BaseComposition1", "BaseComposition2"])
                                           .Root<(int, string)>("Root");
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
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp12 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["(1, 2)"], result);
    }
#endif

    [Fact]
    public async Task ShouldSupportNestedUsing()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           namespace Sample.Models
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency {}
                           
                               interface IService
                               {        
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IEnumerable<IDependency> deps)
                                   {             
                                   }
                               }
                           }

                           namespace Sample
                           {
                               using Models;
                               using Pure.DI;
                               using static Pure.DI.Lifetime;
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("MyNs.Abc.Composition")
                                           .Bind<IDependency>().As(Singleton).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Bind<IEnumerable<IDependency>>().To(ctx => 
                                           {
                                               ctx.Inject<IDependency>(out var dep1);
                                               ctx.Inject<IDependency>(out var dep2);
                                               return new List<IDependency> { dep1, dep2 };
                                           })
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
    public async Task ShouldSupportNonAbstractRoots()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal interface IDependency { }
                           
                               internal class Dependency : IDependency { }
                           
                               internal interface IService { }
                           
                               internal class Service : IService
                               {
                                   public Service(IDependency dependency)
                                   {
                                   }
                               }
                           
                               internal class OtherService : IService
                               {
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition() =>
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind("Other").To<OtherService>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service")
                                           .Root<OtherService>("OtherRoot", "Other");
                               }  
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service);
                                       Console.WriteLine(composition.OtherRoot);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Service", "Sample.OtherService"], result);
    }

    [Fact]
    public async Task ShouldSupportNullableResultTypeWhenFactory()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using static Pure.DI.DI;

                           namespace Sample
                           {
                               interface IDependency { };
                           
                               class Dependency : IDependency { }
                               
                               interface IService
                               {
                                   void DoSomething();
                               }
                               
                               class Service : IService
                               {
                                   public Service(IDependency? dependency)
                                   {
                                   }
                               
                                   public void DoSomething()
                                   {
                                   }
                               }
                           
                               public class Program
                               {
                                   private void Setup() =>
                                        DI.Setup(nameof(Composition))
                                           .Bind<IDependency?>().To<IDependency?>(_ => default)
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                           
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Service"], result);
    }

    [Fact]
    public async Task ShouldSupportPartialMethodRoot()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               partial class Composition
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<string>().To(_ => "Abc")
                                           .Root<string>("Root", default, RootKinds.Partial | RootKinds.Method);
                                   }
                                   
                                   public partial string Root();
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       Console.WriteLine(new Composition().Root());
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportPartialPrivateMethodRoot()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               partial class Composition
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<string>().To(_ => "Abc")
                                           .Root<string>("Root", default, RootKinds.Partial | RootKinds.Method | RootKinds.Private);
                                   }
                                   
                                   private partial string Root();
                                   
                                   public string GetRoot() => Root();
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       Console.WriteLine(new Composition().GetRoot());
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportPartialStaticMethodRoot()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               partial class Composition
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Bind<string>().To(_ => "Abc")
                                           .Root<string>("Root", default, RootKinds.Partial | RootKinds.Static | RootKinds.Method);
                                   }
                                   
                                   public static partial string Root();
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       Console.WriteLine(Composition.Root());
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportRootWhenNamedTagParam()
    {
        // Given

        // When
        var result = await """
                           namespace Sample
                           {
                               using System;
                               using Pure.DI;
                               using Sample;
                           
                               internal interface IService { }
                           
                               internal class Service: IService { }
                               
                               internal partial class Composition
                               {                   
                                   void Setup() => 
                                       DI.Setup("Composition")
                                       .Bind<IService>("Abc").To<Service>()
                                       .Root<IService>(tag: "Abc"); 
                               }       
                           
                               public class Program
                               {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.Resolve<IService>("Abc"));
                                  }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Sample.Service"], result);
    }

    [Fact]
    public async Task ShouldSupportSeveralContracts()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                           
                               class Dependency: IDependency
                               {
                                   [Ordinal(1)]
                                   internal void Initialize([Tag(374)] string depName)
                                   {
                                       Console.WriteLine($"Initialize {depName}");
                                   }
                               }
                               
                               interface IDependency2 {}
                           
                               class Dependency2: IDependency2, IDisposable
                               {
                                   public void Dispose() { }
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                               }
                           
                               class Service: IService 
                               {
                                   private IDependency _dep;
                                   public Service(IDisposable disposable, IDependency dep, IDependency2 dep2)
                                   { 
                                       _dep = dep;
                                   }
                           
                                   public IDependency Dep => _dep;
                                   
                                   public void Run()
                                   {
                                       Console.WriteLine("Run");
                                   }
                           
                                   [Ordinal(7)]
                                   public void Activate()
                                   {
                                       Console.WriteLine("Activate");
                                   }
                           
                                   [Ordinal(1)]
                                   internal void Initialize(IDependency dep)
                                   {
                                       Console.WriteLine("Initialize");
                                       Console.WriteLine(dep != Dep);
                                   }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind<IDisposable>().Bind<IDependency2>().As(Lifetime.PerResolve).To<Dependency2>().Root<IDisposable>("Dependency2")
                                           .Bind<IService>().To<Service>()
                                           .Arg<string>("depName", 374)
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("dep");
                                       var service = composition.Service;                    
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Initialize dep", "Initialize dep", "Initialize", "True", "Activate"], result);
    }

    [Fact]
    public async Task ShouldSupportStaticMethodRoot()
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
                                           .Bind<string>().To(_ => "Abc")
                                           .Root<string>("Root", default, RootKinds.Static | RootKinds.Method);
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       Console.WriteLine(Composition.Root());
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportStaticRoot()
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
                                           .Bind<string>().To(_ => "Abc")
                                           .Root<string>("Root", default, RootKinds.Static);
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       Console.WriteLine(Composition.Root);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Abc"], result);
    }

    [Fact]
    public async Task ShouldSupportStaticSetupMethod()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using static Pure.DI.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       Setup(nameof(Composition))
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service != composition.Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupInPartialClass()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               
                               partial class Setup
                               {
                                   private void Setup1()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               partial class Setup
                               {
                                   private void Setup2()
                                   {
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service is Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupInGlobalNamespace()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           interface IService {}
                           class Service: IService {}
                           
                           static class Setup
                           {
                               private static void SetupComposition()
                               {
                                   DI.Setup("Composition")
                                       .Bind<IService>().To<Service>()
                                       .Root<IService>("Service");
                               }
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.Service is Service);
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithHints()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Hint(Hint.ThreadSafe, "Off")
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service is Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithMultipleBindingsForSameType()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service1: IService {}
                               class Service2: IService {}
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>("1").To<Service1>()
                                           .Bind<IService>("2").To<Service2>()
                                           .Root<IService>("Service1", "1")
                                           .Root<IService>("Service2", "2");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service1 is Service1);
                                       Console.WriteLine(composition.Service2 is Service2);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithDefaultLifetime()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .DefaultLifetime(Lifetime.Singleton)
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service == composition.Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithIgnoredOnCannotResolve()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService 
                               {
                                   public Service(string name) {}
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Hint(Hint.OnCannotResolve, "Ignore")
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       try {
                                           Console.WriteLine(composition.Service == null);
                                       }
                                       catch(Exception) {
                                            Console.WriteLine("Error");
                                       }
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithCustomCompositionNameUsingNameof()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               
                               partial class MyComposition {}

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(MyComposition))
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new MyComposition();
                                       Console.WriteLine(composition is MyComposition);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithNestedClasses()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public class Outer
                               {
                                   public interface IService {}
                                   public class Service: IService {}

                                   static class Setup
                                   {
                                       private static void SetupComposition()
                                       {
                                           DI.Setup("Composition")
                                               .Bind<IService>().To<Service>()
                                               .Root<IService>("Service");
                                       }
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Sample.Composition();
                                       Console.WriteLine(composition.Service is Outer.Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithGenericRoot()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService<T> {}
                               class Service<T>: IService<T> {}
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService<TT>>().To<Service<TT>>()
                                           .Root<IService<int>>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service is Service<int>);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithMultipleRootsOfSameType()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service1")
                                           .Root<IService>("Service2");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service1 is Service);
                                       Console.WriteLine(composition.Service2 is Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ShouldSupportSetupWithConstantName()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               
                               static class Setup
                               {
                                   private const string Name = "Composition";
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(Name)
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service is Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithTag()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>("MyTag").To<Service>()
                                           .Root<IService>("Service", "MyTag");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service is Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithMultipleSetupsInOneFile()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService1 {}
                               class Service1: IService1 {}
                               interface IService2 {}
                               class Service2: IService2 {}
                               
                               static class Setup
                               {
                                   private static void Setup1()
                                   {
                                       DI.Setup("Composition1")
                                           .Bind<IService1>().To<Service1>()
                                           .Root<IService1>("Service");
                                   }

                                   private static void Setup2()
                                   {
                                       DI.Setup("Composition2")
                                           .Bind<IService2>().To<Service2>()
                                           .Root<IService2>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var c1 = new Composition1();
                                       var c2 = new Composition2();
                                       Console.WriteLine(c1.Service is Service1);
                                       Console.WriteLine(c2.Service is Service2);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task DefaultLifetimeInBaseShouldBeRestoredToTransient()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               
                               static class Setup
                               {
                                   private static void SetupBase()
                                   {
                                       DI.Setup("Base")
                                           .Bind<string>().To(_ => "Abc")
                                           .Root<string>("Dummy")
                                           .DefaultLifetime(Lifetime.Singleton);
                                   }

                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .DependsOn("Base")
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service != composition.Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithOverriddenDefaultLifetime()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               
                               static class Setup
                               {
                                   private static void SetupBase()
                                   {
                                       DI.Setup("Base")
                                           .Bind<string>().To(_ => "Abc")
                                           .Root<string>("Dummy")
                                           .DefaultLifetime(Lifetime.Singleton);
                                   }

                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .DependsOn("Base")
                                           .DefaultLifetime(Lifetime.Transient)
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service == composition.Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["False"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithBindToMethod()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>().To(_ => new Service())
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service is Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithGenericBinding()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService<T> {}
                               class Service<T>: IService<T> {}
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService<TT>>().To<Service<TT>>()
                                           .Root<IService<int>>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service is Service<int>);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithPerResolveLifetime()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep {}
                               interface IService { IDep Dep1 { get; } IDep Dep2 { get; } }
                               class Service: IService 
                               {
                                   public Service(IDep dep1, IDep dep2) { Dep1 = dep1; Dep2 = dep2; }
                                   public IDep Dep1 { get; }
                                   public IDep Dep2 { get; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .DefaultLifetime(Lifetime.PerResolve)
                                           .Bind<IDep>().To<Dep>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                       Console.WriteLine(service.Dep1 == service.Dep2);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithPerBlockLifetime()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep {}
                               class Dep: IDep {}
                               interface IService { IDep Dep1 { get; } IDep Dep2 { get; } }
                               class Service: IService 
                               {
                                   public Service(IDep dep1, IDep dep2) { Dep1 = dep1; Dep2 = dep2; }
                                   public IDep Dep1 { get; }
                                   public IDep Dep2 { get; }
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .DefaultLifetime(Lifetime.PerBlock)
                                           .Bind<IDep>().To<Dep>()
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                       Console.WriteLine(service.Dep1 == service.Dep2);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupInStaticConstructor()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               
                               static class Setup
                               {
                                   static Setup()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service is Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupInConstructor()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               
                               class Setup
                               {
                                   public Setup()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service is Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithMultipleInterfacesInOneBind()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService1 {}
                               interface IService2 {}
                               class Service: IService1, IService2 {}
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService1>().Bind<IService2>().To<Service>()
                                           .Root<IService1>("Service1")
                                           .Root<IService2>("Service2");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service1 is Service);
                                       Console.WriteLine(composition.Service2 is Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithPublicRootKind()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service", default, RootKinds.Public | RootKinds.Property);
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service is Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithInternalRootKind()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service", default, RootKinds.Internal | RootKinds.Property);
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service is Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithStaticPropertyRoot()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service", default, RootKinds.Static | RootKinds.Property);
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       Console.WriteLine(Composition.Service is Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithOnNewInstance()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Hint(Hint.OnNewInstance, "On")
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               partial class Composition
                               {
                                   partial void OnNewInstance<T>(ref T value, object? tag, Lifetime lifetime)
                                   {
                                       if (value is Service)
                                       {
                                           Console.WriteLine($"New instance {value.GetType().Name}");
                                       }
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Service;
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Latest));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["New instance Service"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithTransitiveDependsOn()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService1 {}
                               class Service1: IService1 {}
                               interface IService2 {}
                               class Service2: IService2 {}
                               
                               static class Setup
                               {
                                   private static void SetupBase1()
                                   {
                                       DI.Setup("Base1", CompositionKind.Internal)
                                           .Bind<IService1>().To<Service1>();
                                   }

                                   private static void SetupBase2()
                                   {
                                       DI.Setup("Base2", CompositionKind.Internal)
                                           .DependsOn("Base1")
                                           .Bind<IService2>().To<Service2>();
                                   }

                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .DependsOn("Base2")
                                           .Root<IService1>("Service1")
                                           .Root<IService2>("Service2");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service1 is Service1);
                                       Console.WriteLine(composition.Service2 is Service2);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithBindingToTaggedImplementation()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>("Tag").To<Service>()
                                           .Bind<IService>().To(ctx => { ctx.Inject<IService>("Tag", out var service); return service; })
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service is Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithMergingSetupsInDifferentNamespaces()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Ns1
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Sample.Composition")
                                           .Bind<string>().To(_ => "Ns1");
                                   }
                               }
                           }

                           namespace Ns2
                           {
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Sample.Composition")
                                           .Bind<int>().To(_ => 2)
                                           .Root<string>("Str")
                                           .Root<int>("Val");
                                   }
                               }
                           }

                           namespace Sample
                           {
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Str);
                                       Console.WriteLine(composition.Val);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Ns1", "2"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithThreadSafeHint()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Hint(Hint.ThreadSafe, "On")
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service is Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithFormatCodeHint()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Hint(Hint.FormatCode, "On")
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service is Service);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithToStringHint()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService {}
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Hint(Hint.ToString, "On")
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.ToString().Length > 0);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithDisposeHint()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service: IService, IDisposable 
                               {
                                   public void Dispose() => Console.WriteLine("Disposed");
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>().As(Lifetime.Singleton).To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       using (var composition = new Composition())
                                       {
                                           var service = composition.Service;
                                       }
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Disposed"], result);
    }

    [Fact]
    public async Task ShouldSupportSetupWithOverriddenRoot()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IService {}
                               class Service1: IService {}
                               class Service2: IService {}
                               
                               static class Setup
                               {
                                   private static void SetupBase()
                                   {
                                       DI.Setup("Base")
                                           .Bind<IService>().To<Service1>()
                                           .Root<IService>("Service");
                                   }

                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .DependsOn("Base")
                                           .Bind<IService>().To<Service2>();
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service is Service2);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Errors.Count.ShouldBe(0, result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportTypeAliasInBindAndRoot()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using IDep = Sample.IDependency;
                           using Dep = Sample.Dependency;

                           namespace Sample
                           {
                               interface IDependency {}
                               class Dependency: IDependency {}
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().To<Dep>()
                                           .Root<IDep>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service is Dependency);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportTypeAliasForGenericTypeInBind()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using IDep = Sample.IDependency<int>;
                           using Dep = Sample.Dependency<int>;

                           namespace Sample
                           {
                               interface IDependency<T> {}
                               class Dependency<T>: IDependency<T> {}
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().To<Dep>()
                                           .Root<IDep>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service is Dependency<int>);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportTypeAliasInGenericTypeArgument()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using TT1Alias = Pure.DI.TT;

                           namespace Sample
                           {
                               interface IDependency<T> {}
                               class Dependency<T>: IDependency<T> {}
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .GenericTypeArgument<TT1Alias>()
                                           .Bind<IDependency<TT1Alias>>().To<Dependency<TT1Alias>>()
                                           .Root<IDependency<int>>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service is Dependency<int>);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportGlobalTypeAliasInSetup()
    {
        // Given

        // When
        var result = await """
                           global using IDep = Sample.IDependency;
                           global using Dep = Sample.Dependency;
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}
                               class Dependency: IDependency {}
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().To<Dep>()
                                           .Root<IDep>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service is Dependency);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportTypeAliasForNestedType()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using MyService = Sample.Outer.Inner;

                           namespace Sample
                           {
                               public class Outer
                               {
                                   public class Inner {}
                               }
                               
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<MyService>().To<MyService>()
                                           .Root<MyService>("Service");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service is Outer.Inner);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportComplexSetupWithLargeNumberOfBindings()
    {
        // Given
        var code = new StringBuilder();
        code.AppendLine("using System;");
        code.AppendLine("using Pure.DI;");
        code.AppendLine("using System.Collections.Generic;");
        code.AppendLine("namespace Sample {");

        // Many dependencies
        const int count = 100;
        for (var i = 0; i < count; i++)
        {
            code.AppendLine($"    interface IDependency{i} {{ }}");
            code.AppendLine($"    class Dependency{i} : IDependency{i} {{ }}");
        }

        // Diverse mechanisms
        code.AppendLine("    interface IGeneric<T> { T Value { get; } }");
        code.AppendLine("    class GenericImpl<T> : IGeneric<T> { public GenericImpl(T value) => Value = value; public T Value { get; } }");
        code.AppendLine("    interface IAlpha { }");
        code.AppendLine("    interface IBeta { }");
        code.AppendLine("    class AlphaBeta : IAlpha, IBeta { }");
        code.AppendLine("    interface IFactoryGenerated { string Name { get; } }");
        code.AppendLine("    class FactoryGenerated : IFactoryGenerated { public FactoryGenerated(string name) => Name = name; public string Name { get; } }");
        code.AppendLine("    class DependencyWithOrdinal {");
        code.AppendLine("        [Ordinal(1)] public DependencyWithOrdinal([Tag(\"MyTag\")] string s) { }");
        code.AppendLine("        [Ordinal(0)] public DependencyWithOrdinal() { }");
        code.AppendLine("    }");

        // Service depending on everything
        code.AppendLine("    interface IService { void Do(); }");
        code.AppendLine("    class Service : IService {");
        code.Append("        public Service(");
        for (var i = 0; i < count; i++)
        {
            code.Append($"IDependency{i} dep{i}, ");
        }
        code.Append("IGeneric<int> genericInt, IAlpha alpha, IBeta beta, IFactoryGenerated factoryGen, [Tag(\"MyTag\")] string taggedStr, DependencyWithOrdinal depOrdinal");
        code.AppendLine(") { }");
        code.AppendLine("        public void Do() => Console.WriteLine(\"Done\");");
        code.AppendLine("    }");

        code.AppendLine("    static class BaseSetup {");
        code.AppendLine("        private static void SetupBase() {");
        code.AppendLine("            DI.Setup(\"BaseComposition\", CompositionKind.Internal)");
        code.AppendLine("                .Bind<IAlpha, IBeta>().As(Lifetime.Singleton).To<AlphaBeta>();");
        code.AppendLine("        }");
        code.AppendLine("    }");

        code.AppendLine("    static class Setup {");
        code.AppendLine("        private static void SetupComposition() {");
        code.AppendLine("            DI.Setup(\"Composition\")");
        code.AppendLine("                .DependsOn(\"BaseComposition\")");

        for (var i = 0; i < count; i++)
        {
            var lifetime = (i % 5) switch
            {
                0 => "Lifetime.Transient",
                1 => "Lifetime.Singleton",
                2 => "Lifetime.PerResolve",
                3 => "Lifetime.Scoped",
                4 => "Lifetime.PerBlock",
                _ => "Lifetime.Transient"
            };
            code.AppendLine($"                .Bind<IDependency{i}>().As({lifetime}).To<Dependency{i}>()");
        }

        code.AppendLine("                .Bind<IGeneric<TT>>().To<GenericImpl<TT>>()");
        code.AppendLine("                .Bind<int>().To(_ => 10)");
        code.AppendLine("                .Bind<IFactoryGenerated>().To(ctx => new FactoryGenerated(\"Generated\"))");
        code.AppendLine("                .Bind<string>(\"MyTag\").To(_ => \"TaggedValue\")");
        code.AppendLine("                .Bind<DependencyWithOrdinal>().To<DependencyWithOrdinal>()");
        code.AppendLine("                .Bind<IService>().To<Service>()");
        code.AppendLine("                .Root<IService>(\"Root\");");
        code.AppendLine("        }");
        code.AppendLine("    }");

        code.AppendLine("    public class Program {");
        code.AppendLine("        public static void Main() {");
        code.AppendLine("            var composition = new Composition();");
        code.AppendLine("            var root = composition.Root;");
        code.AppendLine("            root.Do();");
        code.AppendLine("        }");
        code.AppendLine("    }");
        code.AppendLine("}");

        // When
        var result = await code.ToString().RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Done"], result);
    }

    [Fact]
    public async Task ShouldHandleDependsOnWithEnum()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           #pragma warning disable CS8618
                           #pragma warning disable CS0649

                           namespace Sample
                           {
                               public class ClockConfig: IClockConfig
                               {
                                   public string? Value { get; set; }
                               }

                               public interface IClockConfig
                               {
                                   string? Value { get; }
                               }

                               public class ClockService
                               {
                                   public ClockService(IClockConfig config)
                                   {
                                   }
                               }

                               public class ClocksComposition
                               {
                                   private ClockConfig? clockConfig = null;

                                   void Setup() => DI.Setup(kind: CompositionKind.Internal)
                                       .Bind<IClockConfig>().To(_ => clockConfig!)
                                       .Singleton<ClockService>();
                               }

                               public partial class Scope
                               {
                                   void Setup() => DI.Setup()
                                       .DependsOn(nameof(ClocksComposition), SetupContextKind.Members)
                                       .Root<ClockService>("ClockManager");
                               }

                               public class Program
                               {
                                   public static void Main()
                               {
                                       var composition = new Scope();
                                       var clockManager = composition.ClockManager;
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
    }
}
