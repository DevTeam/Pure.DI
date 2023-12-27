namespace Pure.DI.IntegrationTests;

[Collection(nameof(IntegrationTestsCollectionDefinition))]
public class BenchmarksTests
{
    private const string Models = """
    namespace Sample;
    using System;
    using System.Collections.Generic;
    using Pure.DI;

    public interface ICompositionRoot
    {
      bool Verify();
    }

    public sealed class CompositionRoot : ICompositionRoot
    {
      public CompositionRoot(
          IService1 service1,
          IService2 service21,
          IService2 service22,
          IService2 service23,
          IService3 service3,
          IService4 service41,
          IService4 service42)
      {
      }

      public bool Verify() => true;
    }

    public interface IService1 { }

    public sealed class Service1 : IService1
    {
        public Service1(IService2 service2)
        {
        }
    }

    public interface IService2 { }

    public sealed class Service2 : IService2
    {
        public Service2(
            IService3 service31,
            IService3 service32,
            IService3 service33,
            IService3 service34,
            IService3 service35)
        {
        }
    }

    public sealed class Service2Array : IService2
    {
        public Service2Array(IService3[] services) { }
    }

    public sealed class Service2Enum : IService2
    {
        public Service2Enum(IEnumerable<IService3> services)
        {
            using var enumerator = services.GetEnumerator();
            enumerator.MoveNext();
        }
    }

    public sealed class Service2Func : IService2
    {
        public Service2Func(Func<IService3> service3Factory)
        {
            service3Factory();
        }
    }

    public interface IService3 { }

    public sealed class Service3 : IService3
    {
        public Service3(IService4 service41, IService4 service42)
        {
        }
    }

    public sealed class Service3v2 : IService3
    {
        public Service3v2(IService4 service41, IService4 service42)
        {
        }
    }

    public sealed class Service3v3 : IService3
    {
        public Service3v3(IService4 service41, IService4 service42)
        {
        }
    }

    public sealed class Service3v4 : IService3
    {
        public Service3v4(IService4 service41, IService4 service42)
        {
        }
    }

    public interface IService4 { }

    public class Service4 : IService4 { }
    
    """;

    [Fact]
    public async Task ShouldSupportFuncWhenPerResolve()
    {
        // Given

        // When
        var result = await (Models + """
        static class Setup
        {
          private static void SetupComposition()
          {
              // FormatCode = On
              DI.Setup(nameof(Func))
                  .Bind<IService1>().To<Service1>()
                  .Bind<IService2>().To<Service2Func>()
                  .Bind<IService3>().To<Service3>()
                  .Bind<IService4>().To<Service4>()
                  .Root<CompositionRoot>("PureDIByCR", default, RootKinds.Method);
          }
        }

        public class Program
        {
          public static void Main()
          {
              var composition = new Func();
          }
        }
        """).RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.GeneratedCode.Split(Environment.NewLine).Count(i => i.Contains("new Sample.Service3(new Sample.Service4(), new Sample.Service4());")).ShouldBe(1);
    }
    
    [Fact]
    public async Task ShouldSupportFuncWhenPerBlock()
    {
        // Given

        // When
        var result = await (Models + """
        static class Setup
        {
          private static void SetupComposition()
          {
              // FormatCode = On
              DI.Setup(nameof(Func))
                    .Bind<Func<TT>>()
                        .As(Lifetime.PerBlock)
                        .To(ctx => new Func<TT>(() =>
                        {
                        ctx.Inject<TT>(ctx.Tag, out var value);
                        return value;
                        }))
                  .Bind<IService1>().To<Service1>()
                  .Bind<IService2>().To<Service2Func>()
                  .Bind<IService3>().To<Service3>()
                  .Bind<IService4>().To<Service4>()
                  .Root<CompositionRoot>("PureDIByCR", default, RootKinds.Method);
          }
        }

        public class Program
        {
          public static void Main()
          {
              var composition = new Func();
          }
        }
        """).RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.GeneratedCode.Split(Environment.NewLine).Count(i => i.Contains("new Sample.Service3(new Sample.Service4(), new Sample.Service4());")).ShouldBe(1);
    }
}