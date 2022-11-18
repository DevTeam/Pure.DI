// ReSharper disable StringLiteralTypo
namespace Pure.DI.Tests.Integration;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public class SetupTests
{
    [Theory]
    [InlineData("partial class")]
    [InlineData("static class")]
    [InlineData("partial struct")]
    [InlineData("struct")]
    [InlineData("partial record")]
    [InlineData("record")]
    public void ShouldChangeComposerNameAddingDIWhenItIsNotPossibleToMakeStaticPartialClass(string classDefinition)
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<string>().To(_ => {
                                return ""abc"";
                            })
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(string value) => Value = value;        
                }
            }".Replace("static partial class", classDefinition).Run(out var generatedCode, new RunOptions
        {
            Statements = @"System.Console.WriteLine(ComposerDI.Resolve<CompositionRoot>().Value);"
        });

        // Then
        output.Count.ShouldBe(1, generatedCode);
        output[0].ShouldBe("abc", generatedCode);
    }

    [Fact]
    public void ShouldUseDefaultValue()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                
                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(string value = ""abc"") => Value = value;
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "abc"
        }, generatedCode);
    }

    [Fact]
    public void ShouldPreferDefaultValueOverNullableOne()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                
                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(string? value, long i = 0) => Value = value ?? ""zyx"";
                    internal CompositionRoot(string value = ""abc"", int i = 0) => Value = value;
                }
            }".Run(out var generatedCode, new RunOptions
        {
            NullableContextOptions = NullableContextOptions.Annotations
        });

        // Then
        output.ShouldBe(new[]
        {
            "abc"
        }, generatedCode);
    }

    [Fact]
    public void ShouldPreferResolvedValueOverDefaultOneWhenParamsCountIsEq()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                
                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<string>().To(_ => ""xyz"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(string value = ""abc"", int i = 0) => Value = value;
                    internal CompositionRoot(string value, long i = 0) => Value = value;
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "xyz"
        }, generatedCode);
    }

    [Fact]
    public void ShouldPreferPublicOverInternal()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                
                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(string value = ""abc"", int i =10) => Value = value;
                    public CompositionRoot(string value = ""xyz"", long i = 10) => Value = value;                    
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "xyz"
        }, generatedCode);
    }

    [Fact]
    public void ShouldPreferResolvedValueOverDefaultOneWhenParamsCountIsNotEq()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                
                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<string>().To(_ => ""xyz"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(int value = 99, int i = 0) => Value = value.ToString();
                    internal CompositionRoot(string value) => Value = value;
                }
            }".Run(out var generatedCode, new RunOptions
        {
            NullableContextOptions = NullableContextOptions.Annotations
        });

        // Then
        output.ShouldBe(new[]
        {
            "xyz"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportNullableDependencies()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                
                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class Dependency 
                {
                    public Dependency(string? value) { }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(string? value, Dependency dependency) => Value = (value ?? ""null"") + "", "" + dependency.ToString();
                }
            }".Run(out var generatedCode, new RunOptions
        {
            NullableContextOptions = NullableContextOptions.Annotations
        });

        // Then
        output.ShouldBe(new[]
        {
            "null, Sample.Dependency"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSetupForNestedClass()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public partial class Foo
                {
                    public static partial class Composer
                    {
                        // Models a random subatomic event that may or may not occur
                        private static readonly Random Indeterminacy = new();

                        static Composer()
                        {
                            DI.Setup()
                                .Bind<string>().To(_ => ""abc"")
                                // Composition Root
                                .Bind<CompositionRoot>().To<CompositionRoot>();
                        }
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(string value) => Value = value;        
                }                
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "abc"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSetupForNestedClassWhenComposerTypeNameIsSpecified()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public partial class Foo
                {
                    public static partial class Composer
                    {
                        // Models a random subatomic event that may or may not occur
                        private static readonly Random Indeterminacy = new();

                        static Composer()
                        {
                            DI.Setup(""Resolver"")
                                .Bind<string>().To(_ => ""abc"")
                                // Composition Root
                                .Bind<CompositionRoot>().To<CompositionRoot>();
                        }
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(string value) => Value = value;        
                }                
            }".Run(out var generatedCode, new RunOptions
        {
            Statements = @"System.Console.WriteLine(Resolver.Resolve<CompositionRoot>().Value);"
        });

        // Then
        output.ShouldBe(new[]
        {
            "abc"
        }, generatedCode);
    }

    [Fact]
    public void ShouldAddPostfixDIWhenCannotUseOwnerClass()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public class Foo
                {
                    public static class Composer
                    {
                        // Models a random subatomic event that may or may not occur
                        private static readonly Random Indeterminacy = new();

                        static Composer()
                        {
                            DI.Setup()
                                .Bind<string>().To(_ => ""abc"")
                                // Composition Root
                                .Bind<CompositionRoot>().To<CompositionRoot>();
                        }
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(string value) => Value = value;        
                }                
            }".Run(out var generatedCode, new RunOptions
        {
            Statements = @"System.Console.WriteLine(ComposerDI.Resolve<CompositionRoot>().Value);"
        });

        // Then
        output.Count.ShouldBe(1, generatedCode);
        output[0].ShouldBe("abc", generatedCode);
    }

    [Fact]
    public void ShouldOverrideBinding()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<string>().To(_ => ""abc"")
                            .Bind<string>().To(_ => ""xyz"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(string value) => Value = value;
                }
            }".Run(out var generatedCode);

        // Then
        output.Any(i => i == "xyz").ShouldBeTrue(generatedCode);
    }

    [Fact]
    public void ShouldBindUsingStatementsLambda()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<string>().Tags(1).To(_ => ""abc"")
                            .Bind<string>().To(ctx => { return ctx.Resolve<string>(1) + ""_xyz""; })
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(string value) => Value = value;        
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "abc_xyz"
        }, generatedCode);
    }

    [Fact]
    public void ShouldUsePredefinedTagAttribute()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<string>().Tags(1).To(_ => ""abc"")
                            .Bind<string>().To(_ => ""xyz"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot([Tag(1)] string value) => Value = value;
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "abc"
        }, generatedCode);
    }
    
    [Fact]
    public void ShouldUsePredefinedTagAttributeWhenTagIsTypeOf()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<string>().Tags(typeof(string)).To(_ => ""abc"")
                            .Bind<string>().To(_ => ""xyz"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot([Tag(typeof(string))] string value) => Value = value;
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "abc"
        }, generatedCode);
    }

    [Fact]
    public void ShouldUsePredefinedTagAttributeAdv()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public interface IMyClass1 {}
                public interface IMyClass2 {}
                    
                public class MyClass: IMyClass1, IMyClass2
                { 
                    public MyClass() {}
                    public MyClass(IMyClass2 myClass2) {}
                }

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IMyClass2>(2).To<MyClass>(_ => new MyClass())
                            .Bind<IMyClass1>().To(ctx => new MyClass(ctx.Resolve<IMyClass2>(2)))
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly IMyClass1 Value;
                    internal CompositionRoot(IMyClass1 value) => Value = value;
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "Sample.MyClass"
        }, generatedCode);
    }

    [Fact]
    public void ShouldUsePredefinedTagAttributeAdvWhenSingleton()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public interface IMyClass1 {}
                public interface IMyClass2 {}
                    
                public class MyClass: IMyClass1, IMyClass2
                { 
                    public MyClass() {}
                    public MyClass(IMyClass1 myClass1, IMyClass2 myClass2) {}
                }

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IMyClass1>().Bind<IMyClass2>().Tags(1).Tags(3).To<MyClass>()
                            .Bind<IMyClass1>().Bind<IMyClass2>().Tags(2, 4).As(Lifetime.Singleton).To<MyClass>(_ => new MyClass())
                            .Bind<IMyClass1>().Bind<IMyClass2>().As(Lifetime.Singleton).To(ctx => new MyClass(ctx.Resolve<IMyClass1>(1), ctx.Resolve<IMyClass2>(2)))
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly IMyClass1 Value;
                    internal CompositionRoot(IMyClass1 value) => Value = value;
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "Sample.MyClass"
        }, generatedCode);
    }

    [Fact]
    public void ShouldUseCustomTagAttribute()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                [AttributeUsage(AttributeTargets.Parameter)]
                public class MyTagAttribute: Attribute
                {
                    public MyTagAttribute(int someVal, object tag) { }
                }

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .TagAttribute<MyTagAttribute>(1)
                            .Bind<string>().Tags(1).To(_ => ""abc"")
                            .Bind<string>().To(_ => ""xyz"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot([MyTag(0, 1)] string value) => Value = value;        
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "abc"
        }, generatedCode);
    }

    [Fact]
    public void ShouldUsePredefinedTypeAttribute()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<string>().To(_ => ""xyz"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot([Type(typeof(string))] object value) => Value = (string)value;        
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "xyz"
        }, generatedCode);
    }

#if !ROSLYN38
    [Fact]
    public void ShouldUsePredefinedGenericTypeAttribute()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<string>().To(_ => ""xyz"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot([Type<string>] object value) => Value = (string)value;        
                }
            }".Run(out var generatedCode, new RunOptions { LanguageVersion = LanguageVersion.Preview });

        // Then
        output.ShouldBe(new[]
        {
            "xyz"
        }, generatedCode);
    }
#endif

    [Fact]
    public void ShouldUseCustomTypeAttribute()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                [AttributeUsage(AttributeTargets.Parameter)]
                public class MyTypeAttribute: Attribute
                {
                    public MyTypeAttribute(int someVal, Type type) { }
                }
                   
                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .TypeAttribute<MyTypeAttribute>(1)
                            .Bind<string>().To(_ => ""xyz"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot([MyType(0, typeof(string))] object value) => Value = (string)value;        
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "xyz"
        }, generatedCode);
    }

    [Fact]
    public void ShouldUsePredefinedOrderAttributeWhenMethod()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<string>().To(_ => ""xyz"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public string Value;
                    internal CompositionRoot() {} 

                    [Order(1)] internal void Init(string value) => Value = value;
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "xyz"
        }, generatedCode);
    }

    [Fact]
    public void ShouldUsePredefinedOrderAttributeWhenField()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<string>().To(_ => ""xyz"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    [Order(1)] internal string Value;
                    internal CompositionRoot() {}                     
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "xyz"
        }, generatedCode);
    }

    [Fact]
    public void ShouldUsePredefinedOrderAttributeWhenProperty()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<string>().To(_ => ""xyz"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    [Order(1)] internal string Value { get; set; }
                    internal CompositionRoot() {}                     
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "xyz"
        }, generatedCode);
    }

    [Fact]
    public void ShouldUseCustomOrderAttributeWhenProperty()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                [AttributeUsage(AttributeTargets.Property)]
                public class MyOrderAttribute: Attribute
                {
                    public MyOrderAttribute(int someVal, int order) { }
                }

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .OrderAttribute<MyOrderAttribute>(1)
                            .Bind<string>().To(_ => ""xyz"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    [MyOrder(0, 1)] internal string Value { get; set; }
                    internal CompositionRoot() {}
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "xyz"
        }, generatedCode);
    }

    [Fact]
    public void ShouldUsePredefinedOrderAttributeWhenCtor()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<string>().To(_ => ""xyz"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public string Value;
                    
                    public CompositionRoot()
                    {
                    }

                    [Order(0)] internal CompositionRoot(string value) => Value = value;                    

                    public CompositionRoot(string value, string val)
                    {
                    }
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "xyz"
        }, generatedCode);
    }

    [Fact]
    public void ShouldUsePredefinedOrderAttributeWhenCtorAndFewAttributes()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<string>().To(_ => ""xyz"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public string Value;
                    
                    public CompositionRoot()
                    {
                    }

                    [Order(0)] internal CompositionRoot(string value) => Value = value;

                    [Order(1)] public CompositionRoot(string value, string val)
                    {
                    }
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "xyz"
        }, generatedCode);
    }

    [Fact]
    public void ShouldUsePredefinedOrderAttributeWhenCtorAndCannotUseMarked()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<string>().To(_ => ""xyz"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public string Value;
                    
                    public CompositionRoot()
                    {
                    }

                    public CompositionRoot(string value) => Value = value;

                    [Order(0)] public CompositionRoot(string value, int val)
                    {
                    }
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "xyz"
        }, generatedCode);
    }

    [Fact]
    public void ShouldResolveInstanceWithoutBinding()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
 
                public class MyClass { }

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(MyClass value) => Value = value.ToString();
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "Sample.MyClass"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportSpecifiedGenerics()
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
                            // Composition Root
                            .Bind<IMyClass<string>>().To<MyClass<string>>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(IMyClass<string> value) => Value = value.ToString();
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "Sample.MyClass`1[System.String]"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportResolveOfGenericDependencies()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
 
                public interface IMyClass2<T> { }
                public class MyClass2<T>: IMyClass2<T> { }

                public interface IMyClass<T> { }
                public class MyClass<T>: IMyClass<T>
                {
                    public MyClass(IMyClass2<int> val)
                    {
                    }
                }

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            // Composition Root
                            .Bind<IMyClass2<TT>>().To<MyClass2<TT>>()
                            .Bind<IMyClass<TT>>().To<MyClass<TT>>()
                            .Bind<ICompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal interface ICompositionRoot { }

                internal class CompositionRoot: ICompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(IMyClass<string> value) => Value = value.ToString();
                }
            }".Run(out var generatedCode, new RunOptions
        {
            Statements =
                @"System.Console.WriteLine(Composer.Resolve<IMyClass2<int>>());
                  System.Console.WriteLine(Composer.Resolve<IMyClass<string>>());"
        });

        // Then
        output.ShouldBe(new[]
        {
            "Sample.MyClass2`1[System.Int32]", "Sample.MyClass`1[System.String]"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportTypeOfT()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
 
                public class MyClass<T>
                {
                    public Type Type;
                    public MyClass(Type type) { Type = type; }
                }

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            // Composition Root
                            .Bind<MyClass<TT>>().To(_ => new MyClass<TT>(typeof(TT)))
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public string Value;
                    internal CompositionRoot(MyClass<string> value) => Value = value.Type.ToString();
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "System.String"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportGenericsWithoutBinding()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
 
                public class MyClass<T> { }

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(MyClass<string> value) => Value = value.ToString();
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "Sample.MyClass`1[System.String]"
        }, generatedCode);
    }

    [Fact]
    public void ShouldUseDependsOn()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                
                static partial class Composer1
                {
                    static Composer1()
                    {
                        DI.Setup()
                            .Bind<string>().To(_ => ""abc"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .DependsOn(nameof(Composer1))
                            .Bind<string>().To(_ => ""xyz"");
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(string value ) => Value = value;        
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "xyz",
            "Warning DIW002: Bind<string>().To(_ => \"abc\") exists and will be overridden by a new one Bind<string>().To(_ => \"xyz\") for string."
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportAnyTag()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<string>().AnyTag().To(_ => ""abc"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot([Tag(1)] string value) => Value = value;
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "abc"
        }, generatedCode);
    }

    [Fact]
    public void ShouldUseFreeMemberNameWhenType()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly bool Value;
                    internal CompositionRoot(Foo value1, Foo value2) => Value = value1 == value2;        
                }

                public class Foo { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<Foo>().As(Pure.DI.Lifetime.Singleton).To<Foo>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }

                    private static class SingletonSampleFoo { }
                }    
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "True"
        }, generatedCode);
    }

    [Fact]
    public void ShouldUseFreeMemberNameWhenContextType()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly bool Value;
                    internal CompositionRoot(Foo value1, Foo value2) => Value = value1 == value2;        
                }

                public class Foo { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<Foo>().As(Pure.DI.Lifetime.Singleton).To<Foo>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }

                    private static class Context { }
                }    
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "True"
        }, generatedCode);
    }

    [Fact]
    public void ShouldUseFreeMemberNameWhenMethod()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly bool Value;
                    internal CompositionRoot(Foo value1, Foo value2) => Value = value1 == value2;        
                }

                public class Foo { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<Foo>().As(Pure.DI.Lifetime.Singleton).To<Foo>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }

                    private static void SingletonSampleFoo() { }
                }    
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "True"
        }, generatedCode);
    }

    [Fact]
    public void ShouldUseFreeMemberNameWhenField()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly bool Value;
                    internal CompositionRoot(Foo value1, Foo value2) => Value = value1 == value2;        
                }

                public class Foo { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<Foo>().As(Pure.DI.Lifetime.Singleton).To<Foo>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();                        
                    }

                    #pragma warning disable 0414
                    private static int SingletonSampleFoo = 0;
                }    
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "True"
        }, generatedCode);
    }

    [Fact]
    public void ShouldUseFreeMemberNameWhenResolversField()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly bool Value;
                    internal CompositionRoot(Foo value1, Foo value2) => Value = value1 == value2;        
                }

                public class Foo { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<Foo>().As(Pure.DI.Lifetime.Singleton).To<Foo>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();                        
                    }

                    #pragma warning disable 0414
                    private static int Resolvers = 0;
                }    
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "True"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportDefaultLifetime()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly bool Value;
                    internal CompositionRoot(Foo value1, Foo value2, Foo2 value3, Foo2 value4) => Value = value1 == value2 && value3 != value4;        
                }

                public class Foo { }

                public class Foo2 { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Default(Pure.DI.Lifetime.Singleton)
                            .Bind<Foo>().To<Foo>()
                            .Default(Pure.DI.Lifetime.Transient)
                            .Bind<Foo2>().To<Foo2>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                    
                }    
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "True"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportDependencyTags()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<string>(1, ""A"").Tags(3).To(_ => ""abc"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot([Tag(1)] string value1, [Tag(""A"")] string value2, [Tag(3)] string value3) => Value = value1 + value2 + value3;
                }
            }".Run(out var generatedCode);

        // Then
        // ReSharper disable once StringLiteralTypo
        output.ShouldBe(new[]
        {
            "abcabcabc"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportDependencyOverrideByTag()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<string>(1, ""A"").To(_ => ""abc"")
                            .Bind<string>(1, ""B"").To(_ => ""xyz"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot() => Value = Composer.ResolveString(1) + Composer.ResolveString(""A"");
                }
            }".Run(out var generatedCode);

        // Then
        // ReSharper disable once StringLiteralTypo
        output.ShouldBe(new[]
        {
            "xyzabc",
            "Warning DIW002: Bind<string>(1, \"A\").To(_ => \"abc\") exists and will be overridden by a new one Bind<string>(1, \"B\").To(_ => \"xyz\") for string(1)."
        }, generatedCode);
    }

    [Fact]
    public void ShouldNotChangeEmptyTagOfResolveWithinFactoryWhenTagIsNotEmpty()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IService>(1).To(ctx => new Service(ctx.Resolve<IDep>()))
                            .Bind<IDep>().To<Dep>()
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                interface IDep {}
                class Dep: IDep { }

                interface IService {}
                class Service: IService
                {
                    public Service(IDep val) { }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot([Tag(1)] IService value) => Value = value.ToString();
                }
            }".Run(out var generatedCode);

        // Then
        // ReSharper disable once StringLiteralTypo
        output.ShouldBe(new[]
        {
            "Sample.Service"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportRecord()
    {
        // Given

        // When
        var output = @"
            using System;
            using static Pure.DI.Lifetime;
            using Pure.DI;

            namespace Sample
            {
                // Let's create an abstraction

                interface IBox<out T> { T Content { get; } }

                enum State { Alive, Dead }

                // Here is our implementation

                class CardboardBox<T> : IBox<T>
                {
                    public CardboardBox(T content) => Content = content;

                    public T Content { get; }

                    public override string ToString() => $""[{ Content}]"";
                }

                record ShroedingersCat(Lazy<State> Superposition) : ICat
                {
                    public State State => Superposition.Value;
                }

                // Let's glue all together

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            // Models a random subatomic event that may or may not occur
                            .Bind<Random>().As(Singleton).To<Random>()
                            // Represents a quantum superposition of 2 states: Alive or Dead
                            .Bind<State>().To(ctx => (State)ctx.Resolve<Random>().Next(2))
                            // Represents schrodinger's cat
                            .Bind<ICat>().To<ShroedingersCat>();

                        DI.Setup()
                            // Represents a cardboard box with any content
                            .Bind<IBox<TT>>().To<CardboardBox<TT>>()
                            // Composition Root
                            .Bind<CompositionRoot>().As(Singleton).To<CompositionRoot>();
                    }
                }

                // Time to open boxes!

                internal class CompositionRoot
                {
                    public readonly IBox<ICat> Value;
                    internal CompositionRoot(IBox<ICat> box) => Value = box;
                }
            }".Run(out var generatedCode, new RunOptions
        {
            AdditionalCode =
            {
                "namespace Sample { interface ICat { State State { get; } } }"
            }
        });

        // Then
        (output.Contains("[ShroedingersCat { Superposition = Value is not created., State = Dead }]") || output.Contains("[ShroedingersCat { Superposition = Value is not created., State = Alive }]")).ShouldBeTrue(generatedCode);
    }
    
    [Fact]
    public void ShouldSupportInitProperty()
    {
        // Given

        // When
        var output = @"
            #pragma warning disable CS8618
            namespace Sample
            {
                using System;
                using Pure.DI;
                
                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IDependency>().To<Dependency>()
                            .Bind<MyService>().To<MyService>()
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal interface IDependency
                {
                    int Index { get; set; }
                }

                internal class Dependency: IDependency
                {
                    public int Index { get; set; }
                }

                internal class MyService
                {
                    [Order(0)] public IDependency Dependency { get; init; }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(MyService myService) => Value = myService.Dependency.ToString();
                }
            #pragma warning restore CS8618
            }".Run(
                out var generatedCode,
                new RunOptions
                {
                    NullableContextOptions = NullableContextOptions.Annotations,
                    LanguageVersion = LanguageVersion.CSharp9
                });

        // Then
        output.ShouldBe(new[]
        {
            "Sample.Dependency"
        }, generatedCode);
    }
    
    [Theory]
    [InlineData("public")]
    [InlineData("internaL")]
    [InlineData("protected")]
    [InlineData("Private")]
    [InlineData("ProtectedAndInternal")]
    [InlineData("ProtectedOrInternal")]
    public void ShouldChangeAccessibility(string accessibility)
    {
        // Given

        // When
        @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                static partial class Composer
                {
                    static Composer()
                    {
                        // Accessibility=accessibilityValue
                        DI.Setup()
                            .Bind<string>().To(_ => ""abc"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(string value) => Value = value;        
                }
            }"
            .Replace("accessibilityValue", accessibility)
            // Then
            .Run(out _, new RunOptions {Statements = string.Empty});
    }
    
    [Fact]
    public void ShouldSupportMEDIWhenMEDIIsTrue()
    {
        // Given

        // When
        @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using Microsoft.Extensions.DependencyInjection;
                
                static partial class Composer
                {
                    static Composer()
                    {
                        // MEDI=true
                        DI.Setup()
                            .Bind<string>().To(_ => ""abc"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(string value) => Value = value;        
                }
            }"
            // Then
            .Run(out var generatedCode, new RunOptions {Statements = "new Microsoft.Extensions.DependencyInjection.ServiceCollection().AddComposer();"});
        
        generatedCode.Contains("AddComposer(").ShouldBeTrue();
    }
    
    [Fact]
    public void ShouldNotSupportMEDIWhenMEDIIsFalse()
    {
        // Given

        // When
        @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using Microsoft.Extensions.DependencyInjection;
                
                static partial class Composer
                {
                    static Composer()
                    {
                        // MEDI=false
                        DI.Setup()
                            .Bind<string>().To(_ => ""abc"")
                            // Composition Root
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }

                internal class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(string value) => Value = value;        
                }
            }"
            // Then
            .Run(out var generatedCode);
        
        generatedCode.Contains("AddComposer(").ShouldBeFalse();
    }
}