namespace Pure.DI.IntegrationTests;

#pragma warning disable CA1861

public class FactoryTypeRewriterIntegrationTests
{
    [Fact]
    public async Task ShouldSupportBuilderWhenSetupAndServiceInDifferentSyntaxTrees()
    {
        var result = await new[]
        {
            """
            using System;
            using Pure.DI;

            namespace Sample
            {
                interface IDependency { }

                static class Setup
                {
                    private static void SetupComposition()
                    {
                        DI.Setup("Composition")
                            .Bind().To<Dependency>()
                            .Builder<Service>("BuildUpService");
                    }
                }

                public class Program
                {
                    public static void Main()
                    {
                        var composition = new Composition();
                        var service = new Service();
                        composition.BuildUpService(service);
                        Console.WriteLine(service.Dep != null);
                    }
                }
            }
            """,
            """
            using Pure.DI;

            namespace Sample
            {
                class Dependency : IDependency { }

                class Service
                {
                    [Ordinal(0)]
                    public IDependency? Dep { get; set; }
                }
            }
            """
        }.RunAsync();

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportGenericBuilderWhenContractsAndImplementationsInDifferentSyntaxTrees()
    {
        var result = await new[]
        {
            """
            using System;
            using Pure.DI;

            namespace Sample
            {
                interface IService<T, T2>
                {
                    T Id { get; }
                }

                static class Setup
                {
                    private static void SetupComposition()
                    {
                        DI.Setup("Composition")
                            .Bind(Tag.Id).To<TT>(_ => (TT)(object)Guid.NewGuid())
                            .Builders<IService<TT, TT2>>("BuildUpGeneric");
                    }
                }

                public class Program
                {
                    public static void Main()
                    {
                        var composition = new Composition();
                        var service = composition.BuildUpGeneric(new Service<Guid, int>());
                        Console.WriteLine(service.Id != Guid.Empty);
                    }
                }
            }
            """,
            """
            using Pure.DI;

            namespace Sample
            {
                class Service<T, T2> : IService<T, T2>
                    where T : struct
                {
                    public T Id { get; private set; }

                    [Ordinal(0)]
                    public void SetId([Tag(Tag.Id)] T id) => Id = id;
                }
            }
            """
        }.RunAsync();

        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }
}
