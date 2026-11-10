namespace Pure.DI.IntegrationTests;

/// <summary>
/// The variables of the generated code are named after the types of their instances, with a number for each next variable
/// of a type. So the second variable of the type Service1 and the first variable of the type Service11 got the same name.
/// </summary>
public class VariableNamesTests
{
    [Fact]
    public async Task ShouldDeclareUniqueVariablesWhenNameOfTypeEndsWithNumber()
    {
        // Given

        // When
        var result = await """
            using System;
            using Pure.DI;

            namespace Sample
            {
                sealed class Service1 { }

                sealed class Service11 { }

                sealed class Service
                {
                    public Service(Service1 first, Service1 second, Service11 other)
                    {
                        Valid = !ReferenceEquals(first, second);
                    }

                    public bool Valid { get; }
                }

                partial class Composition
                {
                    private static void Setup() => DI.Setup(nameof(Composition))
                        .Bind<Service1>().To(_ => { var service = new Service1(); return service; })
                        .Bind<Service11>().To(_ => { var service = new Service11(); return service; })
                        .Root<Service>("Root");
                }

                public static class Program
                {
                    public static void Main() => Console.WriteLine(new Composition().Root.Valid);
                }
            }
            """.RunAsync(new Options(Options.ModernLanguageVersion));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldDeclareUniqueRootsWhenNameOfTypeEndsWithNumber()
    {
        // Given

        // When
        var result = await """
            using System;
            using Pure.DI;

            namespace Sample
            {
                sealed class Service1 { }

                sealed class Service11 { }

                partial class Composition
                {
                    private static void Setup() => DI.Setup(nameof(Composition))
                        .Bind<Service1>("a", "b").To<Service1>()
                        .Root<Service1>("", "a")
                        .Root<Service1>("", "b")
                        .Root<Service11>();
                }

                public static class Program
                {
                    public static void Main()
                    {
                        var composition = new Composition();
                        Console.WriteLine($"{composition.Resolve<Service1>("a").GetType().Name} {composition.Resolve<Service1>("b").GetType().Name} {composition.Resolve<Service11>().GetType().Name}");
                    }
                }
            }
            """.RunAsync(new Options(Options.ModernLanguageVersion));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Service1 Service1 Service11"], result);
    }
}
