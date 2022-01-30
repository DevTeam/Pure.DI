namespace Pure.DI.Tests.Integration;

using Microsoft.CodeAnalysis.CSharp;

public class FactoryTests
{
    [Fact]
    public void ShouldImplementDisposingViaFactory()
    {
        // Given
        const string? statements = "var root = Composer.Resolve<CompositionRoot>();" +
                                   "System.Console.WriteLine(root.Value.IsDisposed);" +
                                   "Composer.Resolve<DisposingLifetime<MyClass>>().Dispose();" +
                                   "System.Console.WriteLine(root.Value.IsDisposed);";

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using System.Collections.Generic;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly MyClass Value;
                    internal CompositionRoot(MyClass value) => Value = value;
                }

                public class DisposingLifetime<T>: IFactory<T>, IDisposable
                {
                    private readonly HashSet<IDisposable> _disposables = new ();

                    public T Create(Func<T> factory, Type implementationType, object tag)
                    {
                        var instance = factory();
                        if (instance is IDisposable disposable)
                        {
                            _disposables.Add(disposable);
                        }

                        return instance;
                    }
                    
                    public void Dispose()
                    {
                        foreach(var disposable in _disposables)
                        {
                            disposable.Dispose();
                        }

                        _disposables.Clear();
                    }
                }

                public class MyClass: IDisposable
                {
                    public bool IsDisposed;
                    
                    public void Dispose()
                    {
                        IsDisposed = true;
                    }
                }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IFactory<MyClass>>().Bind<DisposingLifetime<MyClass>>().As(Lifetime.Singleton).To<DisposingLifetime<MyClass>>()
                            .Bind<MyClass>().To<MyClass>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }
            }".Run(out var generatedCode, new RunOptions
        {
            Statements = statements
        });

        // Then
        output.ShouldBe(new[]
        {
            "False", "True"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportFactory()
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
                    public readonly string Value;
                    internal CompositionRoot(string value) => Value = value;
                }

                public class MyLifetime: IFactory<string>
                {
                    public string Create(Func<string> factory, Type implementationType, object tag)
                    {
                        return factory() + ""_abc"";
                    }
                }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IFactory<string>>().To<MyLifetime>()
                            .Bind<string>().To(_ => ""xyz"")
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                    
                }    
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "xyz_abc"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportFactoryWhenFilter()
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
                    public readonly string Value;
                    internal CompositionRoot(string value) => Value = value;
                }

                [Include(""string"")]
                public class MyLifetime<T>: IFactory<T>
                {
                    public T Create(Func<T> factory, Type implementationType, object tag)
                    {
                        return (T)(object)(factory().ToString() + ""_abc"");
                    }
                }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IFactory<TT>>().To<MyLifetime<TT>>()
                            .Bind<string>().To(_ => ""xyz"")
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                    
                }    
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "xyz_abc"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportFactoryWhenTag()
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
                    public readonly string Value;
                    internal CompositionRoot([Tag(1)] string value1, string value2) => Value = value1 + value2;
                }

                public class MyLifetime: IFactory<string>
                {
                    public string Create(Func<string> factory, Type implementationType, object tag)
                    {
                        return factory() + ""_abc"";
                    }
                }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IFactory<string>>().Tags(1).To<MyLifetime>()
                            .Bind<string>().Tags(1).To(_ => ""xyz"")
                            .Bind<string>().To(_ => ""123"")
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                    
                }    
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "xyz_abc123"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportMethodFactory()
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
                    public readonly string Value;
                    internal CompositionRoot(string value) => Value = value;
                }

                [Include(""string"")]
                public class MyFactory: IFactory
                {
                    public T Create<T>(Func<T> factory, Type implementationType, object tag)
                    {
                        return (T)(object)(factory() as string + ""_abc"");
                    }
                }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IFactory>().To<MyFactory>()
                            .Bind<string>().To(_ => ""xyz"")
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                    
                }    
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "xyz_abc"
        }, generatedCode);
    }

    [Fact]
    public void ShouldIntercept()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using System.Collections.Generic;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly IMyInterface Value;
                    internal CompositionRoot([Tag(1)] IMyInterface value) { Value = value; }
                }

                public interface IMyInterface {}
                public class MyClass: IMyInterface { }
                public interface IMyInterface2 {}
                public class MyClass2: IMyInterface2 { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IFactory<TT>>(1).As(Lifetime.Singleton).To<MyFactory<TT>>()
                            .Bind<IFactory<TT>>().As(Lifetime.Singleton).To<MyFactory<TT>>()
                            .Bind<IMyInterface>(1).To<MyClass>()
                            .Bind<IMyInterface2>().To<MyClass2>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }

                    [Exclude(""MyClass2"")]
                    public class MyFactory<T>: IFactory<T>
                    {
                        public MyFactory(IMyInterface2 val2) { }
                        public T Create(Func<T> factory, Type implementationType, object tag)
                        {
                            System.Console.WriteLine(implementationType.Name + "": "" + typeof(T).Name + ""("" +tag+ "")"");
                            return factory();
                        }
                    }
                }    
            }".Run(out var generatedCode, new RunOptions
        {
            LanguageVersion = LanguageVersion.CSharp4
        });

        // Then
        output.ShouldBe(new[]
        {
            "CompositionRoot: CompositionRoot()", "MyClass: IMyInterface(1)", "Sample.MyClass"
        }, generatedCode);
    }

    [Fact]
    public void ShouldInterceptMany()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using System.Collections.Generic;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly IMyInterface1 Value;
                    internal CompositionRoot(IMyInterface1 value1, [Tag(2)] IMyInterface2 value2) { Value = value1; }
                }

                public class MyFactory: IFactory
                {
                    public T Create<T>(Func<T> factory, Type implementationType, object tag)
                    {
                        System.Console.WriteLine(implementationType.Name + "": "" + typeof(T).Name + ""("" +tag+ "")"");
                        return factory();
                    }
                }

                public interface IMyInterface1 {}
                public interface IMyInterface2 {}
                public class MyClass: IMyInterface1, IMyInterface2 { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IFactory>().As(Lifetime.Singleton).To<MyFactory>()
                            .Bind<IMyInterface1>().Bind<IMyInterface2>(2).To<MyClass>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }    
            }".Run(out var generatedCode, new RunOptions
        {
            LanguageVersion = LanguageVersion.CSharp4
        });

        // Then
        output.ShouldBe(new[]
        {
            "CompositionRoot: CompositionRoot()", "MyClass: IMyInterface1()", "MyClass: IMyInterface2(2)", "Sample.MyClass"
        }, generatedCode);
    }

    [Fact]
    public void ShouldInterceptManyWhenFunc()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using System.Collections.Generic;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly IMyInterface1 Value;
                    internal CompositionRoot(IMyInterface1 value1, [Tag(2)] IMyInterface2 value2) => Value = value1;
                }

                public class MyFactory: IFactory
                {
                    public T Create<T>(Func<T> factory, Type implementationType, object tag)
                    {
                        System.Console.WriteLine($""{implementationType.Name}: {typeof(T).Name}({tag})"");
                        return factory();
                    }
                }

                public interface IMyInterface1 {}
                public interface IMyInterface2 {}
                public class MyClass: IMyInterface1, IMyInterface2 { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IFactory>().As(Lifetime.Singleton).To<MyFactory>()
                            .Bind<MyClass>().To<MyClass>()
                            .Bind<IMyInterface1>().Bind<IMyInterface2>(2).To(ctx => new MyClass())
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }    
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "CompositionRoot: CompositionRoot()", "IMyInterface1: IMyInterface1()", "IMyInterface2: IMyInterface2(2)", "Sample.MyClass"
        }, generatedCode);
    }
}