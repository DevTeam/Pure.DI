namespace Pure.DI.Tests.Integration
{
    using System.Linq;
    using Core;
    using Shouldly;
    using Xunit;

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
            }".Replace("static partial class", classDefinition).Run(out var generatedCode, new RunOptions { Statements = @"System.Console.WriteLine(ComposerDI.Resolve<CompositionRoot>().Value);" });

            // Then
            output.ShouldBe(new[] { "abc" }, generatedCode);
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
            output.ShouldBe(new[] { "abc" }, generatedCode);
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
            output.ShouldBe(new[] { "abc" }, generatedCode);
        }

        [Fact]
        public void ShouldSetupForNestedClassWhenTargetTypeNameIsSpecified()
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
            }".Run(out var generatedCode, new RunOptions { Statements = @"System.Console.WriteLine(Resolver.Resolve<CompositionRoot>().Value);" });

            // Then
            output.ShouldBe(new[] { "abc" }, generatedCode);
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
            }".Run(out var generatedCode, new RunOptions { Statements = @"System.Console.WriteLine(ComposerDI.Resolve<CompositionRoot>().Value);" });

            // Then
            output.ShouldBe(new[] { "abc" }, generatedCode);
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
            output.Any(i => i.Contains(Diagnostics.BindingIsAlreadyExist)).ShouldBeTrue(generatedCode);
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
                            .Bind<string>().Tag(1).To(_ => ""abc"")
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
            output.ShouldBe(new[] { "abc_xyz" }, generatedCode);
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
                            .Bind<string>().Tag(1).To(_ => ""abc"")
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
            output.ShouldBe(new[] { "abc" }, generatedCode);
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
                            .Bind<string>().Tag(1).To(_ => ""abc"")
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
            output.ShouldBe(new[] { "abc" }, generatedCode);
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
            output.ShouldBe(new[] { "xyz" }, generatedCode);
        }

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
            output.ShouldBe(new[] { "xyz" }, generatedCode);
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
            output.ShouldBe(new[] { "xyz" }, generatedCode);
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
            output.ShouldBe(new[] { "xyz" }, generatedCode);
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
            output.ShouldBe(new[] { "xyz" }, generatedCode);
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
            output.ShouldBe(new[] { "xyz" }, generatedCode);
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
            output.ShouldBe(new[] { "xyz" }, generatedCode);
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
            output.ShouldBe(new[] { "xyz" }, generatedCode);
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

                    internal CompositionRoot(string value) => Value = value;                    

                    [Order(0)] public CompositionRoot(string value, int val)
                    {
                    }
                }
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "xyz" }, generatedCode);
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
            output.ShouldBe(new[] { "Sample.MyClass" }, generatedCode);
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
            output.ShouldBe(new[] { "Sample.MyClass`1[System.String]" }, generatedCode);
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
            }".Run(out var generatedCode, new RunOptions { Statements = 
                @"System.Console.WriteLine(Composer.Resolve<IMyClass2<int>>());
                  System.Console.WriteLine(Composer.Resolve<IMyClass<string>>());"
            });

            // Then
            output.ShouldBe(new[] { "Sample.MyClass2`1[System.Int32]", "Sample.MyClass`1[System.String]" }, generatedCode);
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
            output.ShouldBe(new[] { "System.String" }, generatedCode);
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
            output.ShouldBe(new[] { "Sample.MyClass`1[System.String]" }, generatedCode);
        }
    }
}