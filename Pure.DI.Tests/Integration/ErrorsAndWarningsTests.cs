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
            output.Any(i => i.Contains(Diagnostics.Error.CannotResolve)).ShouldBeTrue(generatedCode);
        }
        
        [Fact]
        public void ShouldShowCompilationErrorWhenCannotResolveDependency()
        {
            // Given

            // When
            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;
                using static Pure.DI.Lifetime;

                public interface IDeepDependency {}

                public interface IDependency {}

                public class Dependency: IDependency { public Dependency(IDeepDependency deepDependency) {} }

                public interface IService {}

                public class Service: IService { public Service(IDependency dependency) {} }

                public class CompositionRoot
                {
                    public readonly IService Value;
                    internal CompositionRoot(IService value) {}
                }
                
                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IDependency>().To<Dependency>()
                            .Bind<IService>().To<Service>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                   
                }    
            }".Run(out var generatedCode);

            // Then
            output.Any(i => i.Contains(Diagnostics.Error.CannotResolve)).ShouldBeTrue(generatedCode);
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
                    public MyClass(MyClass2 val) { }
                }

                public class MyClass2
                {
                    private MyClass2() { }
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
                            .Bind<MyClass2>().To<MyClass2>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                   
                }    
            }".Run(out var generatedCode);

            // Then
            output.Any(i => i.Contains(Diagnostics.Error.CannotResolve)).ShouldBeTrue(generatedCode);
            output.Any(i => i.Contains("MyClass")).ShouldBeTrue(generatedCode);
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