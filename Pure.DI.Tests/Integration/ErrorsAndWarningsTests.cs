namespace Pure.DI.Tests.Integration
{
    using System.Linq;
    using Core;
    using Shouldly;
    using Xunit;

    public class ErrorsAndWarningsTests
    {
        [Theory]
        [InlineData("int")]
        [InlineData("string")]
        public void ShouldShowCompilationErrorWhenCannotResolve(string type)
        {
            // Given

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using static Pure.DI.Lifetime;

                public class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(string value) => Value = value;
                }
                
                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                   
                }    
            }".Replace("string", type).Run(out var generatedCode);

            // Then
            output.Any(i => i.Contains(Diagnostics.Error.CannotResolveDependency)).ShouldBeTrue(generatedCode);
        }

        [Theory]
        [InlineData("int")]
        [InlineData("string")]
        public void ShouldShowCompilationWarningWhenCannotResolve(string type)
        {
            // Given

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using static Pure.DI.Lifetime;

                public class CompositionRoot
                {
                    public readonly string Value;
                    internal CompositionRoot(string value) => Value = value;
                }

                public class Fallback: IFallback
                {
                    public object Resolve(Type type, object tag) => throw new Exception(""Cannot resolve!!!"");                  
                }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IFallback>().To<Fallback>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }           

                    private static object Fallback(Type type, object tag) => throw new InvalidOperationException(""Cannot resolve!!!"");
                }    
            }".Replace("string", type).Run(out var generatedCode);

            // Then
            output.Any(i => i.Contains(Diagnostics.Warning.CannotResolveDependency)).ShouldBeTrue(generatedCode);
            output.Any(i => i.Contains("Cannot resolve!!!")).ShouldBeTrue(generatedCode);
        }

        [Fact]
        public void ShouldDetectCircularDependency()
        {
            // Given

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using static Pure.DI.Lifetime;

                public class MyClass
                {
                    public MyClass(MyClass value) { }
                }

                public class CompositionRoot
                {
                    public readonly MyClass Value;
                    internal CompositionRoot(MyClass value) { }
                }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<MyClass>().To<MyClass>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                   
                }    
            }".Run(out var generatedCode);

            // Then
            output.Any(i => i.Contains(Diagnostics.Error.CircularDependency)).ShouldBeTrue(generatedCode);
        }

        [Fact]
        public void ShouldHandleCannotFindCtor()
        {
            // Given

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using static Pure.DI.Lifetime;

                public class MyClass
                {
                    private MyClass() { }
                }

                public class CompositionRoot
                {
                    public readonly MyClass Value;
                    internal CompositionRoot(MyClass value) { }
                }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<MyClass>().To<MyClass>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                   
                }    
            }".Run(out var generatedCode);

            // Then
            output.Any(i => i.Contains(Diagnostics.Error.CannotFindCtor)).ShouldBeTrue(generatedCode);
        }

        [Fact]
        public void ShouldHandleCannotFindCtor2()
        {
            // Given

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using static Pure.DI.Lifetime;

                public class MyClass
                {
                    [Obsolete]
                    public MyClass() { }
                }

                public class CompositionRoot
                {
                    public readonly MyClass Value;
                    internal CompositionRoot(MyClass value) { }
                }

                #pragma warning disable 0612
                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<MyClass>().To<MyClass>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                   
                }    
            }".Run(out var generatedCode);

            // Then
            output.Any(i => i.Contains(Diagnostics.Warning.CtorIsObsoleted)).ShouldBeTrue(generatedCode);
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

                    [Order(1)] private void Init(string value) => Value = value;
                }
            }".Run(out var generatedCode);

            // Then
            output.Any(i => i.Contains(Diagnostics.Error.MemberIsInaccessible)).ShouldBeTrue(generatedCode);
        }
    }
}