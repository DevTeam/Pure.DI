namespace Pure.DI.Tests.Integration
{
    using Shouldly;
    using Xunit;

    public class FactoryTests
    {
        [Fact]
        public void ShouldImplementDisposingViaFactory()
        {
            // Given
            const string? statements = "var root = Composer.Resolve<CompositionRoot>();" +
                                       "System.Console.WriteLine(root.Value.IsDisposed);" +
                                       "Composer.MyLifetime.Dispose();" +
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

                    public T Create(Func<T> factory)
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
                    internal static readonly DisposingLifetime<MyClass> MyLifetime = new ();

                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IFactory<MyClass>>().As(Lifetime.Singleton).To(_ => MyLifetime)
                            .Bind<MyClass>().To<MyClass>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                    
                }    
            }".Run(out var generatedCode, new RunOptions { Statements = statements});

            // Then
            output.ShouldBe(new[] { "False", "True" }, generatedCode);
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
                    public string Create(Func<string> factory)
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
            output.ShouldBe(new[] { "xyz_abc" }, generatedCode);
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
                    public string Create(Func<string> factory)
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
            output.ShouldBe(new[] { "xyz_abc123" }, generatedCode);
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
                    public T Create<T>(Func<T> factory, object tag)
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
            output.ShouldBe(new[] { "xyz_abc" }, generatedCode);
        }
    }
}