namespace Pure.DI.IntegrationTests;

using Core;

/// <summary>
/// Tests related to the usage of hints for customizing the generation process.
/// </summary>
public class HintsTests
{
    [Fact]
    public async Task ShouldNotUseDefaultCtorWhenSkipDefaultConstructorIsOnHasNoAndOther()
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
                                   public ShroedingersCat() { }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Hint(Hint.SkipDefaultConstructor, "On")
                                           .Bind<ICat>().To<ShroedingersCat>()
                                           .Bind<IBox<TT>>().To<CardboardBox<TT>>() 
                                           .Root<Program>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   IBox<ICat> _box;
                           
                                   internal Program(IBox<ICat> box) => _box = box;
                           
                                   private void Run() => Console.WriteLine(_box);
                           
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       composition.Root.Run();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve && i.Locations.FirstOrDefault().GetSource() == "content").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldNotUseDefaultCtorWhenSkipDefaultConstructorIsOn()
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
                                   public ShroedingersCat() { }
                                   
                                   public ShroedingersCat(int id) { }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Hint(Hint.SkipDefaultConstructor, "On")
                                           .Bind<ICat>().To<ShroedingersCat>()
                                           .Bind<IBox<TT>>().To<CardboardBox<TT>>() 
                                           .Root<Program>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   IBox<ICat> _box;
                           
                                   internal Program(IBox<ICat> box) => _box = box;
                           
                                   private void Run() => Console.WriteLine(_box);
                           
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       composition.Root.Run();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve && i.Locations.FirstOrDefault().GetSource() == "id").ShouldBe(1, result);
    }

    [Theory]
    [InlineData("*", true)]
    [InlineData("ShroedingersCat", false)]
    [InlineData("*ShroedingersCat", true)]
    [InlineData("CardboardBox", false)]
    public async Task ShouldUseDefaultCtorWhenSkipDefaultConstructorAndWildcardWasApplied(string wildcard, bool hasError)
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
                                   public ShroedingersCat() { }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Hint(Hint.SkipDefaultConstructor, "On")
                                           .Hint(Hint.SkipDefaultConstructorImplementationTypeNameWildcard, "#wildcard#")
                                           .Bind<ICat>().To<ShroedingersCat>()
                                           .Bind<IBox<TT>>().To<CardboardBox<TT>>() 
                                           .Root<Program>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   IBox<ICat> _box;
                           
                                   internal Program(IBox<ICat> box) => _box = box;
                           
                                   private void Run() => Console.WriteLine(_box);
                           
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       composition.Root.Run();
                                   }
                               }
                           }
                           """.Replace("#wildcard#", wildcard).RunAsync();

        // Then
        result.Success.ShouldBe(!hasError, result);
        if (hasError)
        {
            result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve && i.Locations.FirstOrDefault().GetSource() == "content").ShouldBe(1, result);
        }
    }

    [Fact]
    public async Task ShouldNotUseDefaultCtorWhenSkipDefaultConstructorIsOnAndHasNoBinding()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IBox<out T> { T Content { get; } }
                           
                               class CardboardBox<T> : IBox<T>
                               {
                                   public CardboardBox(T content) => Content = content;
                           
                                   public T Content { get; }
                           
                                   public override string ToString() => $"[{Content}]";
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Hint(Hint.SkipDefaultConstructor, "On")
                                           .Bind<IBox<TT>>().To<CardboardBox<TT>>() 
                                           .Root<Program>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   IBox<ShroedingersCat> _box;
                           
                                   internal Program(IBox<ShroedingersCat> box) => _box = box;
                           
                                   private void Run() => Console.WriteLine(_box);
                           
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       composition.Root.Run();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve && i.Locations.FirstOrDefault().GetSource() == "content").ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldNotAutoBindWhenDisableAutoBindingIsOn()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IBox<out T> { T Content { get; } }
                           
                               class CardboardBox<T> : IBox<T>
                               {
                                   public CardboardBox(T content) => Content = content;
                           
                                   public T Content { get; }
                           
                                   public override string ToString() => $"[{Content}]";
                               }
                           
                               class ShroedingersCat
                               {
                                   public ShroedingersCat() { }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Hint(Hint.DisableAutoBinding, "On")
                                           .Bind<IBox<TT>>().To<CardboardBox<TT>>() 
                                           .Bind<Program>().To<Program>()
                                           .Root<Program>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   IBox<ShroedingersCat> _box;
                           
                                   internal Program(IBox<ShroedingersCat> box) => _box = box;
                           
                                   private void Run() => Console.WriteLine(_box);
                           
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       composition.Root.Run();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve && i.Locations.FirstOrDefault().GetSource() == "content").ShouldBe(1, result);
    }

    [Theory]
    [InlineData("*", true)]
    [InlineData("ShroedingersCat", false)]
    [InlineData("*ShroedingersCat", true)]
    [InlineData("CardboardBox", false)]
    public async Task ShouldNotAutoBindWhenDisableAutoBindingIsOnAndWildcardWasApplied(string wildcard, bool hasError)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IBox<out T> { T Content { get; } }
                           
                               class CardboardBox<T> : IBox<T>
                               {
                                   public CardboardBox(T content) => Content = content;
                           
                                   public T Content { get; }
                           
                                   public override string ToString() => $"[{Content}]";
                               }
                           
                               class ShroedingersCat
                               {
                                   public ShroedingersCat() { }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Hint(Hint.DisableAutoBinding, "On")
                                           .Hint(Hint.DisableAutoBindingImplementationTypeNameWildcard, "#wildcard#")
                                           .Bind<IBox<TT>>().To<CardboardBox<TT>>() 
                                           .Bind<Program>().To<Program>()
                                           .Root<Program>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   IBox<ShroedingersCat> _box;
                           
                                   internal Program(IBox<ShroedingersCat> box) => _box = box;
                           
                                   private void Run() => Console.WriteLine(_box);
                           
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       composition.Root.Run();
                                   }
                               }
                           }
                           """.Replace("#wildcard#", wildcard).RunAsync();

        // Then
        result.Success.ShouldBe(!hasError, result);
        if (hasError)
        {
            result.Errors.Count(i => i.Id == LogId.ErrorUnableToResolve && i.Locations.FirstOrDefault().GetSource() == "content").ShouldBe(1, result);
        }
    }
    [Fact]
    public async Task ShouldNotGenerateResolveMethodsWhenResolveIsOff()
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

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Hint(Hint.Resolve, "Off")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Root<IDependency>("Root");
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.GeneratedCode.Contains("public T Resolve<T>()").ShouldBeFalse(result);
    }

    [Fact]
    public async Task ShouldNotGenerateThreadSafeSingletonWhenThreadSafeIsOff()
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

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Hint(Hint.ThreadSafe, "Off")
                                           .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                           .Root<IDependency>("Root");
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.GeneratedCode.Contains("lock").ShouldBeFalse(result);
    }
}