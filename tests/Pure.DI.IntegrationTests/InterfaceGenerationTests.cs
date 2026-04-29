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
        var result = await """
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
                           """.RunAsync(new Options(LanguageVersion.CSharp10, CheckCompilationErrors: false));

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

    [Fact]
    public async Task ShouldGenerateSeveralInterfacesWithSelectiveMembers()
    {
        var result = await """
            using Pure.DI;

            namespace Demo;

            public partial interface IReadService
            {
            }

            public partial interface IWriteService
            {
            }

            [GenerateInterface(interfaceName: nameof(IReadService))]
            public partial class Service
            {
                [GenerateInterface(interfaceName: nameof(IReadService))]
                public string Read() => "ok";

                [GenerateInterface(interfaceName: nameof(IWriteService))]
                public void Write(string value)
                {
                }

                [GenerateInterface(interfaceName: nameof(IReadService))]
                [GenerateInterface(interfaceName: nameof(IWriteService))]
                public int Shared => 42;

                public string Unmarked => "hidden";
            }

            public class Program
            {
                public static void Main() { }
            }
            """.RunAsync(new Options(LanguageVersion.CSharp10, CheckCompilationErrors: false));

        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);
        result.GeneratedCode.ShouldContain("public partial interface IReadService");
        result.GeneratedCode.ShouldContain("public partial interface IWriteService");
        result.GeneratedCode.ShouldContain("string Read()");
        result.GeneratedCode.ShouldContain("void Write(string value);");
        result.GeneratedCode.ShouldContain("int Shared { get; }");
        result.GeneratedCode.ShouldNotContain("string Unmarked { get; }");
    }

    [Fact]
    public async Task ShouldGenerateInterfacesFromMemberAttributesWithoutClassAttribute()
    {
        var result = await """
            using Pure.DI;

            namespace Demo;

            public partial interface IAuditReader
            {
            }

            public partial interface IAuditWriter
            {
            }

            public partial class AuditService
            {
                [GenerateInterface(interfaceName: nameof(IAuditReader))]
                public string Read() => "audit";

                [GenerateInterface(interfaceName: nameof(IAuditWriter))]
                public void Write(string message)
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
        result.GeneratedCode.ShouldContain("public partial interface IAuditReader");
        result.GeneratedCode.ShouldContain("public partial interface IAuditWriter");
        result.GeneratedCode.ShouldContain("string Read()");
        result.GeneratedCode.ShouldContain("void Write(string message);");
    }

    [Fact]
    public async Task ShouldPrioritizeIgnoreInterfaceWhenMemberMarkedForSeveralInterfaces()
    {
        var result = await """
            using Pure.DI;

            namespace Demo;

            public partial interface IAlpha
            {
            }

            public partial interface IBeta
            {
            }

            [GenerateInterface(interfaceName: nameof(IAlpha))]
            [GenerateInterface(interfaceName: nameof(IBeta))]
            public partial class Service
            {
                [GenerateInterface(interfaceName: nameof(IAlpha))]
                [GenerateInterface(interfaceName: nameof(IBeta))]
                [IgnoreInterface]
                public string HiddenForAll => "x";

                [GenerateInterface(interfaceName: nameof(IAlpha))]
                public int VisibleForAlpha => 1;

                [GenerateInterface(interfaceName: nameof(IBeta))]
                public int VisibleForBeta => 2;
            }

            public class Program
            {
                public static void Main() { }
            }
            """.RunAsync(new Options(LanguageVersion.CSharp10, CheckCompilationErrors: false));

        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);
        result.GeneratedCode.ShouldContain("public partial interface IAlpha");
        result.GeneratedCode.ShouldContain("public partial interface IBeta");
        result.GeneratedCode.ShouldContain("int VisibleForAlpha { get; }");
        result.GeneratedCode.ShouldContain("int VisibleForBeta { get; }");
        result.GeneratedCode.ShouldNotContain("HiddenForAll");
    }

    [Fact]
    public async Task ShouldNotGenerateInterfaceFromNonPublicMemberAttributes()
    {
        var result = await """
            using Pure.DI;

            namespace Demo;

            public partial interface IRestrictedApi
            {
            }

            public partial class Service
            {
                [GenerateInterface(interfaceName: nameof(IRestrictedApi))]
                private string PrivateData => "secret";

                [GenerateInterface(interfaceName: nameof(IRestrictedApi))]
                protected string ProtectedData => "secret";

                [GenerateInterface(interfaceName: nameof(IRestrictedApi))]
                internal string InternalData => "secret";
            }

            public class Program
            {
                public static void Main() { }
            }
            """.RunAsync(new Options(LanguageVersion.CSharp10, CheckCompilationErrors: false));

        result.Errors.Count.ShouldBe(0, result);
        result.GeneratedCode.ShouldContain("public partial interface IRestrictedApi");
        result.GeneratedCode.ShouldNotContain("PrivateData");
        result.GeneratedCode.ShouldNotContain("ProtectedData");
        result.GeneratedCode.ShouldNotContain("InternalData");
    }

    [Fact]
    public async Task ShouldNotGenerateInterfaceFromStaticMemberAttributes()
    {
        var result = await """
            using Pure.DI;

            namespace Demo;

            public partial interface IStaticApi
            {
            }

            public partial class Service
            {
                [GenerateInterface(interfaceName: nameof(IStaticApi))]
                public static string Name => "static";
            }

            public class Program
            {
                public static void Main() { }
            }
            """.RunAsync(new Options(LanguageVersion.CSharp10, CheckCompilationErrors: false));

        result.Errors.Count.ShouldBe(0, result);
        result.GeneratedCode.ShouldContain("public partial interface IStaticApi");
        result.GeneratedCode.ShouldNotContain("string Name { get; }");
    }

    [Fact]
    public async Task ShouldIncludeOnlyEligibleMembersWhenIneligibleMembersAreMarked()
    {
        var result = await """
            using Pure.DI;

            namespace Demo;

            public partial interface IMixedApi
            {
            }

            public partial class Service
            {
                [GenerateInterface(interfaceName: nameof(IMixedApi))]
                private string Hidden => "x";

                [GenerateInterface(interfaceName: nameof(IMixedApi))]
                public string Visible => "ok";
            }

            public class Program
            {
                public static void Main() { }
            }
            """.RunAsync(new Options(LanguageVersion.CSharp10, CheckCompilationErrors: false));

        result.Errors.Count.ShouldBe(0, result);
        result.GeneratedCode.ShouldContain("public partial interface IMixedApi");
        result.GeneratedCode.ShouldContain("string Visible { get; }");
        result.GeneratedCode.ShouldNotContain("Hidden");
    }
}
