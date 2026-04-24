namespace Pure.DI.IntegrationTests;

public class InterfaceGenerationTests
{
    [Fact]
    public async Task ShouldGenerateAnInterfaceFromAnnotatedClass()
    {
        var result = await """
            using System;
            using Pure.DI;

            namespace Demo;

            public partial interface IService
            {
            }

            [GenerateInterface]
            public partial class Service
            {
                public string Name { get; set; } = string.Empty;

                public event EventHandler? Changed;

                [IgnoreInterface]
                public void Hidden() { }

                public string GetText<T>(string? value)
                    where T : class
                    => value ?? string.Empty;
            }

            public class Program
            {
                public static void Main() { }
            }
            """.RunAsync(new Options(LanguageVersion.CSharp10, CheckCompilationErrors: false));

        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);

        result.GeneratedCode.ShouldContain("public partial interface IService");
        result.GeneratedCode.ShouldContain("string Name { get; set; }");
        result.GeneratedCode.ShouldContain("Changed");
        result.GeneratedCode.ShouldContain("GetText<T>");
        result.GeneratedCode.ShouldNotContain("Hidden");
    }

    [Fact]
    public async Task ShouldGenerateInterfaceForGenericType()
    {
        var result = await """
            using System;
            using Pure.DI;

            namespace Demo;

            public partial interface IRepository<TItem>
            {
            }

            [GenerateInterface]
            public partial class Repository<TItem>
                where TItem : class, new()
            {
                public TItem? Current { get; set; }

                public event EventHandler<TItem>? Created;

                public TItem Create() => new();
            }

            public class Program
            {
                public static void Main() { }
            }
            """.RunAsync(new Options(LanguageVersion.CSharp10, CheckCompilationErrors: false));

        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);

        result.GeneratedCode.ShouldContain("public partial interface IRepository<TItem>");
        result.GeneratedCode.ShouldContain("where TItem : class, new()");
        result.GeneratedCode.ShouldContain("TItem? Current { get; set; }");
        result.GeneratedCode.ShouldContain("EventHandler<TItem>");
    }

    [Fact]
    public async Task ShouldUseGeneratedInterfaceWithPureDi()
    {
        var code = """
            using Pure.DI;

            namespace Demo;

            public partial interface IService
            {
            }

            [GenerateInterface]
            public partial class Service : IService
            {
                public string Message => "ok";
            }

            public partial class Consumer
            {
                public Consumer(IService service)
                {
                    Message = service.Message;
                }

                public string Message { get; }
            }

            partial class Setup
            {
                private static void Configure() =>
                    DI.Setup(nameof(Composition))
                        .Bind<IService>().To<Service>()
                        .Root<Consumer>(nameof(Consumer));
            }

            public class Program
            {
                public static void Main()
                {
                    var composition = new Composition();
                    var consumer = composition.Consumer;
                    System.Console.WriteLine(consumer.Message);
                }
            }
            """;

        var result = await code.RunAsync(new Options(LanguageVersion.CSharp10, CheckCompilationErrors: false));

        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldContain("ok");
    }

    [Fact]
    public async Task ShouldPreserveMethodParameterOrder()
    {
        var result = await """
            using Pure.DI;

            namespace Demo;

            public partial interface IService
            {
            }

            [GenerateInterface]
            public partial class Service
            {
                public void Configure(int first, string second, double third)
                {
                }
            }

            public class Program
            {
                public static void Main() { }
            }
            """.RunAsync(new Options(LanguageVersion.CSharp10, CheckCompilationErrors: false));

        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);

        result.GeneratedCode.ShouldContain("void Configure(int first, string second, double third);");
    }

    [Fact]
    public async Task ShouldNotGenerateInterfaceForSimilarAttributeName()
    {
        var result = await """
            using System;
            using Pure.DI;

            namespace Demo;

            [AttributeUsage(AttributeTargets.Class)]
            public sealed class GenerateInterfaceLikeAttribute : Attribute
            {
            }

            [GenerateInterfaceLike]
            public partial class Service
            {
                public string Name => "demo";
            }

            public class Program
            {
                public static void Main() { }
            }
            """.RunAsync(new Options(LanguageVersion.CSharp10, CheckCompilationErrors: false));

        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);

        result.GeneratedCode.ShouldNotContain("partial interface IService");
    }

    [Fact]
    public async Task ShouldNotIgnoreMembersForSimilarIgnoreAttributeName()
    {
        var result = await """
            using System;
            using Pure.DI;

            namespace Demo;

            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event)]
            public sealed class IgnoreInterfaceLikeAttribute : Attribute
            {
            }

            public partial interface IService
            {
            }

            [GenerateInterface]
            public partial class Service
            {
                [IgnoreInterfaceLike]
                public void ShouldStay()
                {
                }
            }

            public class Program
            {
                public static void Main() { }
            }
            """.RunAsync(new Options(LanguageVersion.CSharp10, CheckCompilationErrors: false));

        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);

        result.GeneratedCode.ShouldContain("void ShouldStay();");
    }

    [Fact]
    public async Task ShouldSupportNamedGenerateInterfaceArgumentsInAnyOrder()
    {
        var result = await """
            using Pure.DI;

            namespace Demo;

            [GenerateInterface(interfaceName: "IMyService", namespaceName: "Demo.Contracts", asInternal: true)]
            public partial class Service
            {
                public string Value => "ok";
            }

            public class Program
            {
                public static void Main() { }
            }
            """.RunAsync(new Options(LanguageVersion.CSharp10, CheckCompilationErrors: false));

        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);
        result.GeneratedCode.ShouldContain("namespace Demo.Contracts");
        result.GeneratedCode.ShouldContain("internal partial interface IMyService");
        result.GeneratedCode.ShouldContain("string Value { get; }");
    }

    [Fact]
    public async Task ShouldSupportNamedGenerateInterfaceArgumentsInDifferentOrder()
    {
        var result = await """
            using Pure.DI;

            namespace Demo;

            [GenerateInterface(asInternal: true, namespaceName: "Demo.Contracts", interfaceName: "IMyService2")]
            public partial class Service2
            {
                public int Count => 7;
            }

            public class Program
            {
                public static void Main() { }
            }
            """.RunAsync(new Options(LanguageVersion.CSharp10, CheckCompilationErrors: false));

        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);
        result.GeneratedCode.ShouldContain("namespace Demo.Contracts");
        result.GeneratedCode.ShouldContain("internal partial interface IMyService2");
        result.GeneratedCode.ShouldContain("int Count { get; }");
    }
}
