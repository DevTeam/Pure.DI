﻿namespace Pure.DI.IntegrationTests;

using Core;

public class ArgsTests
{

    [Theory]
    [InlineData("\"my_id\"", "id_int_my_id")]
    [InlineData("null", "id_int_")]
    [InlineData("\"\"", "id_int_")]
    [InlineData("\"1my_id\"", "id_int_1my_id")]
    [InlineData("\"my   Id\"", "id_int_myId")]
    [InlineData("\"my.Id\"", "id_int_myId")]
    [InlineData("\"my<Id\"", "id_int_my_Id")]
    [InlineData("\"my>Id\"", "id_int_myId")]
    [InlineData("\"my[Id\"", "id_int_my_Id")]
    [InlineData("\"my]Id\"", "id_int_myId")]
    [InlineData("\"my`Id\"", "id_int_myId")]
    public async Task ShouldSupportRootArgWithNameTemplate(string tag, string argName)
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
                              
                              class Dependency2
                              {
                                   public Dependency2([Tag(#tag#)] int id) {}
                              }
                           
                              interface IService
                              {
                                  IDependency Dep { get; }
                           
                                  string Name { get; }
                              }
                           
                              class Service: IService
                              {
                                  public Service(IDependency dep, [Tag(#tag#)] int id, string name)
                                  {
                                      Dep = dep;
                                      Name = name + "_" + id;
                                  }
                           
                                  public IDependency Dep { get; }
                           
                                  public string Name { get; private set; }
                              }
                           
                              static class Setup
                              {
                                  private static void SetupComposition()
                                  {
                                      // ToString = On
                                      DI.Setup("Composition")
                                          .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                          .Bind<IService>().To<Service>()
                                          .Root<Dependency2>()
                                          .RootArg<string>("serviceName_{type}")
                                          .RootArg<int>("id_{type}_{tag}", #tag#)
                                          .Root<IService>("GetService");
                                  }
                              }
                           
                              public class Program
                              {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.GetService(serviceName_string: "Some Name", #argName#: 99).Name);
                                  }
                              }
                           }
                           """.Replace("#tag#", tag).Replace("#argName#", argName).RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(2, result);
        result.Warnings.Count(i => i.Id == LogId.WarningRootArgInResolveMethod && i.Locations.FirstOrDefault().GetSource() == "Root<Dependency2>()").ShouldBe(1, result);
        result.Warnings.Count(i => i.Id == LogId.WarningRootArgInResolveMethod && i.Locations.FirstOrDefault().GetSource() == "Root<IService>(\"GetService\")").ShouldBe(1, result);
        result.StdOut.ShouldBe(["Some Name_99"], result);
    }

    [Fact]
    public async Task ShouldNotShowWaningAboutRootArgWhenResolveMethodsAreNotGenerated()
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
                           
                                   string Name { get; }
                               }
                           
                               class Service: IService
                               {
                                   public Service(IDependency dep, string name)
                                   {
                                       Dep = dep;
                                       Name = name;
                                   }
                           
                                   public IDependency Dep { get; }
                           
                                   public string Name { get; private set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Hint(Hint.Resolve, "Off")
                                           .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .RootArg<string>("serviceName")
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service("Some Name").Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Some Name"], result);
    }

    [Fact]
    public async Task ShouldSupportArg()
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
                           
                                   string Name { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency dep, string name)
                                   { 
                                       Dep = dep;
                                       Name = name;
                                   }
                           
                                   public IDependency Dep { get; }
                           
                                   public string Name { get; private set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Arg<string>("serviceName")
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("Some Name");
                                       Console.WriteLine(composition.Service.Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Some Name"], result);
    }
    [Fact]
    public async Task ShouldSupportArgAsRoot()
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
                                           .Arg<string>("serviceName")
                                           .Root<string>("ServiceName");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("Some Name");
                                       Console.WriteLine(composition.ServiceName);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Some Name"], result);
    }

    [Fact]
    public async Task ShouldSupportArgWhenFewConsumers()
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
                           
                                   string Name { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency dep, string name)
                                   { 
                                       Dep = dep;
                                       Name = name;
                                   }
                           
                                   public IDependency Dep { get; }
                           
                                   public string Name { get; private set; }
                               }
                               
                               class Service2 
                               {
                                   public Service2(string name)
                                   { 
                                       Name = name;
                                   }
                           
                                   public string Name { get; private set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Arg<string>("serviceName")
                                           .Root<IService>("Service")
                                           .Root<Service2>("Service2");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("Some Name");
                                       Console.WriteLine(composition.Service.Name);
                                       Console.WriteLine(composition.Service2.Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Some Name", "Some Name"], result);
    }

    [Fact]
    public async Task ShouldSupportArgWhenFewDeps()
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
                           
                                   string Name { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency dep, string name, string name2)
                                   { 
                                       Dep = dep;
                                       Name = name;
                                   }
                           
                                   public IDependency Dep { get; }
                           
                                   public string Name { get; private set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Arg<string>("serviceName")
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("Some Name");
                                       Console.WriteLine(composition.Service.Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Some Name"], result);
    }

    [Fact]
    public async Task ShouldSupportArgWithNameTemplate()
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
                           
                                   string Name { get; }
                               }
                           
                               class Service: IService 
                               {
                                   public Service(IDependency dep, string name)
                                   { 
                                       Dep = dep;
                                       Name = name;
                                   }
                           
                                   public IDependency Dep { get; }
                           
                                   public string Name { get; private set; }
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Arg<string>("serviceName_{type}")
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition(serviceName_string: "Some Name");
                                       Console.WriteLine(composition.Service.Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Some Name"], result);
    }

    [Fact]
    public async Task ShouldSupportRootArg()
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
                              
                              class Dependency2
                              {
                                   public Dependency2(int id) {}
                              }
                           
                              interface IService
                              {
                                  IDependency Dep { get; }
                           
                                  string Name { get; }
                              }
                           
                              class Service: IService
                              {
                                  public Service(IDependency dep, int id, string name)
                                  {
                                      Dep = dep;
                                      Name = name + "_" + id;
                                  }
                           
                                  public IDependency Dep { get; }
                           
                                  public string Name { get; private set; }
                              }
                           
                              static class Setup
                              {
                                  private static void SetupComposition()
                                  {
                                      // ToString = On
                                      DI.Setup("Composition")
                                          .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                          .Bind<IService>().To<Service>()
                                          .Root<Dependency2>()
                                          .RootArg<string>("serviceName")
                                          .RootArg<int>("id")
                                          .Root<IService>("GetService");
                                  }
                              }
                           
                              public class Program
                              {
                                  public static void Main()
                                  {
                                      var composition = new Composition();
                                      Console.WriteLine(composition.GetService("Some Name", 99).Name);
                                  }
                              }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeFalse(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(2, result);
        result.Warnings.Count(i => i.Id == LogId.WarningRootArgInResolveMethod && i.Locations.FirstOrDefault().GetSource() == "Root<IService>(\"GetService\")").ShouldBe(1, result);
        result.Warnings.Count(i => i.Id == LogId.WarningRootArgInResolveMethod && i.Locations.FirstOrDefault().GetSource() == "Root<Dependency2>()").ShouldBe(1, result);
        result.StdOut.ShouldBe(["Some Name_99"], result);
    }

    [Fact]
    public async Task ShouldSupportSeveralArgs()
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
                                   int _id;
                           
                                   public Dependency(int id)
                                   { 
                                       _id = id;
                                   }
                           
                                   public override string ToString() => _id.ToString();
                               }
                           
                               interface IService
                               {
                                   IDependency Dep { get; }
                           
                                   string Name { get; }
                               }
                           
                               class Service: IService 
                               {
                                   int _id;
                                   string _name;
                           
                                   public Service(IDependency dep, [Tag(99)] int id, string name)
                                   { 
                                       Dep = dep;
                                       _id = id;
                                       _name = name;
                                   }
                           
                                   public IDependency Dep { get; }
                           
                                   public string Name => $"{_name} {_id} {Dep}";
                               }
                           
                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                                           .Bind<IService>().To<Service>()
                                           .Arg<string>("serviceName")
                                           .Arg<int>("id", 99)
                                           .Arg<int>("depId")
                                           .Root<IService>("Service");
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition("Some Name", 37, 56);
                                       Console.WriteLine(composition.Service.Name);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Some Name 37 56"], result);
    }

#if ROSLYN4_8_OR_GREATER
    [Fact]
    public async Task ShouldSupportTagsAsArray()
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
                                           .Arg<int>("x")
                                           .Arg<int>("y", [1, 2])
                                           .Root<int>("Root")
                                           .Root<int>("Root2", 1)
                                           .Root<int>("Root3", 2);
                                   }
                               }
                           
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition(-1, 1);
                                       Console.WriteLine(composition.Root);
                                       Console.WriteLine(composition.Root2);
                                       Console.WriteLine(composition.Root3);
                                   }
                               }
                           }
                           """.RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp12 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["-1", "1", "1"], result);
    }
#endif
}