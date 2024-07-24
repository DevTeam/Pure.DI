namespace Pure.DI.IntegrationTests;

public class RootBindTests
{
    [Fact]
    public async Task ShouldSupportRootBind()
    {
        // Given

        // When
        var result = await """
using System;
using System.Collections.Generic;
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
                .RootBind<IDependency>().To<Dependency>();
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service = composition.Resolve<IDependency>();
            Console.WriteLine(service.GetType() == typeof(Dependency));
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithRootName()
    {
        // Given

        // When
        var result = await """
using System;
using System.Collections.Generic;
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
                .RootBind<IDependency>("Root").To<Dependency>();
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service = composition.Root;
            Console.WriteLine(service.GetType() == typeof(Dependency));
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithRootKind()
    {
        // Given

        // When
        var result = await """
using System;
using System.Collections.Generic;
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
                .RootBind<IDependency>("Root", RootKinds.Method).To<Dependency>();
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service = composition.Root();
            Console.WriteLine(service.GetType() == typeof(Dependency));
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSupportRootBindWithTags()
    {
        // Given

        // When
        var result = await """
using System;
using System.Collections.Generic;
using Pure.DI;
using static Pure.DI.Lifetime;

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
                .RootBind<IDependency>("Root", RootKinds.Property, "RootTag", "Dep2").As(Singleton).To<Dependency>()
                .Root<IDependency>("Root2", "Dep2")
                .Root<IDependency>("Root3");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            Console.WriteLine(composition.Root == composition.Root2);
            Console.WriteLine(composition.Root != composition.Root3);
        }
    }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

#if ROSLYN4_8_OR_GREATER    
    [Fact]
    public async Task ShouldSupportRootBindWithTagsAsNamedArgument()
    {
        // Given

        // When
        var result = await """
using System;
using System.Collections.Generic;
using Pure.DI;
using static Pure.DI.Lifetime;

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
                .RootBind<IDependency>(tags: ["RootTag", "Dep2"]).As(Singleton).To<Dependency>()
                .Root<IDependency>("Root2", "Dep2")
                .Root<IDependency>("Root3");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            Console.WriteLine(composition.Resolve<IDependency>("RootTag") == composition.Root2);
            Console.WriteLine(composition.Resolve<IDependency>("RootTag") != composition.Root3);
        }
    }
}
""".RunAsync(new Options { LanguageVersion = LanguageVersion.CSharp12 });

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }
#endif
}
