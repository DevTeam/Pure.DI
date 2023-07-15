namespace Pure.DI.IntegrationTests;

[Collection(nameof(NonParallelTestsCollectionDefinition))]
public class TagsTests
{
    [Fact]
    public async Task ShouldSupportTags()
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
                .Bind<string>().To(_ => "1")
                .Bind<string>(2).To(_ => "2")
                .Bind<string>('3').To(_ => "3")
                .Bind<string>("4").To(_ => "4")
                .Root<string>("Result")
                .Root<string>("Result2", 2)
                .Root<string>("Result3", '3')
                .Root<string>("Result4", "4");
        }
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            Console.WriteLine(composition.Result);
            Console.WriteLine(composition.Result2);                                           
            Console.WriteLine(composition.Result3);
            Console.WriteLine(composition.Result4);
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("1", "2", "3", "4"), result.GeneratedCode);
    }
    
    [Fact]
    public async Task ShouldSupportTagsWhenEnum()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    public enum MyEnum
    {
        Option2
    };

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<string>().To(_ => "1")
                .Bind<string>(MyEnum.Option2).To(_ => "2")
                .Root<string>("Result")
                .Root<string>("Result2", MyEnum.Option2);
        }
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            Console.WriteLine(composition.Result);                                           
            Console.WriteLine(composition.Result2);
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("1", "2"), result.GeneratedCode);
    }
    
    [Fact]
    public async Task ShouldSupportTagsWhenType()
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
                .Bind<string>().To(_ => "1")
                .Bind<string>(typeof(int)).To(_ => "2")
                .Root<string>("Result")
                .Root<string>("Result2", typeof(int));
        }
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            Console.WriteLine(composition.Result);                                           
            Console.WriteLine(composition.Result2);
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("1", "2"), result.GeneratedCode);
    }
    
    [Fact]
    public async Task ShouldSupportTagsWhenNull()
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
                .Bind<string>(1, null).To(_ => "1")
                .Bind<string>(2).To(_ => "2")
                .Root<string>("Result")
                .Root<string>("Result2", 2);
        }
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            Console.WriteLine(composition.Result);                                           
            Console.WriteLine(composition.Result2);
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("1", "2"), result.GeneratedCode);
    }
    
    [Fact]
    public async Task ShouldSupportTagsWhenDefault()
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
                .Bind<string>(1, default).To(_ => "1")
                .Bind<string>(2).To(_ => "2")
                .Root<string>("Result")
                .Root<string>("Result2", 2);
        }
    }          

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            Console.WriteLine(composition.Result);                                           
            Console.WriteLine(composition.Result2);
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        result.StdOut.ShouldBe(ImmutableArray.Create("1", "2"), result.GeneratedCode);
    }
}