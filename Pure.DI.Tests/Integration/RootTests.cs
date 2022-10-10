namespace Pure.DI.Tests.Integration;

using System.Collections.Generic;

public class RootTests
{
    [Fact]
    public void ShouldSupportGenericRoot()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
 
                public interface IMyClass<T> { }
                public class MyClass<T>: IMyClass<T> { }

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Root<IMyClass<string>>()
                            .Root<IMyClass<int>>()
                            .Bind<IMyClass<TT>>().To<MyClass<TT>>();
                    }
                }
            }".Run(
            out var generatedCode,
            new RunOptions
            {
                Statements = "System.Console.WriteLine(Composer.Resolve<IMyClass<string>>());System.Console.WriteLine(Composer.Resolve<IMyClass<int>>());"
            });

        // Then
        output.ShouldBe(new[]
        {
            "Sample.MyClass`1[System.String]",
            "Sample.MyClass`1[System.Int32]"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportRootWithTag()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
 
                public interface IMyClass<T> { }
                public class MyClass<T>: IMyClass<T> { }

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Root<IMyClass<string>>(""str"")
                            .Root<IMyClass<int>>()
                            .Bind<IMyClass<TT>>().To<MyClass<TT>>()
                            .Bind<IMyClass<TT>>().Tags(""str"").To<MyClass<TT>>();
                    }
                }
            }".Run(
            out var generatedCode,
            new RunOptions
            {
                Statements = "System.Console.WriteLine(Composer.Resolve<IMyClass<string>>(\"str\"));System.Console.WriteLine(Composer.Resolve<IMyClass<int>>());"
            });

        // Then
        output.ShouldBe(new[]
        {
            "Sample.MyClass`1[System.String]",
            "Sample.MyClass`1[System.Int32]"
        }, generatedCode);
    }
    
    [Fact]
    public void ShouldSupportArrayRoot()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using System.Collections.Generic;
                using System.Collections.Immutable;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Root<string[]>()
                            .Bind<string>().Tags(1).To(_ => ""1"")
                            .Bind<string>().To(_ => ""2"")
                            .Bind<string>().Tags(3).To(_ => ""3"");
                    }
                }
            }"
            .Run(
                out var generatedCode,
                new RunOptions
                {
                    Statements = $"System.Console.WriteLine(string.Join(\".\", Composer.Resolve<string[]>()));"
                });

        // Then
        output.Count.ShouldBe(1, generatedCode);
        new HashSet<string>(output[0].Split(".")).SetEquals(new HashSet<string>
        {
            "1",
            "2",
            "3"
        }).ShouldBeTrue(output[0]);
    }
    
    [Theory]
    [InlineData("System.Collections.Generic.IEnumerable")]
    [InlineData("System.Collections.Generic.ICollection")]
    [InlineData("System.Collections.Generic.Stack")]
    [InlineData("System.Collections.Immutable.ImmutableArray")]
    [InlineData("System.Collections.Immutable.IImmutableList")]
    [InlineData("System.Collections.Immutable.ImmutableList")]
    [InlineData("System.Collections.Immutable.IImmutableSet")]
    [InlineData("System.Collections.Immutable.ImmutableQueue")]
    public void ShouldSupportCollectionsRoot(string collectionType)
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using System.Collections.Generic;
                using System.Collections.Immutable;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Root<ICollection<string>>()
                            .Bind<string>().Tags(1).To(_ => ""1"")
                            .Bind<string>().To(_ => ""2"")
                            .Bind<string>().Tags(3).To(_ => ""3"");
                    }
                }
            }".Replace("ICollection", collectionType)
            .Run(
                out var generatedCode,
                new RunOptions
                {
                    Statements = $"System.Console.WriteLine(string.Join(\".\", Composer.Resolve<{collectionType}<string>>()));"
                });

        // Then
        output.Count.ShouldBe(1, generatedCode);
        new HashSet<string>(output[0].Split(".")).SetEquals(new HashSet<string>
        {
            "1",
            "2",
            "3"
        }).ShouldBeTrue(output[0]);
    }
    
    [Fact]
    public void ShouldSupportTuple()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
 
                public interface IMyClass<T> { }
                public class MyClass<T>: IMyClass<T> { }

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Root<(IMyClass<string>, int)>()
                            .Bind<int>().To(_ => 99)
                            .Bind<IMyClass<TT>>().To<MyClass<TT>>();
                    }
                }
            }".Run(
            out var generatedCode,
            new RunOptions
            {
                Statements = "System.Console.WriteLine(Composer.Resolve<(IMyClass<string>, int)>());"
            });

        // Then
        output.ShouldBe(new[]
        {
            "(Sample.MyClass`1[System.String], 99)"
        }, generatedCode);
    }
}