namespace Pure.DI.Tests.Integration
{
    using Shouldly;
    using Xunit;

    public class BindingLifetimeTests
    {
        [Fact]
        public void ShouldImplementDisposingViaCustomLifetime()
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

                public class DisposingLifetime<T>: ILifetime<T>, IDisposable
                {
                    private readonly HashSet<IDisposable> _disposables = new ();

                    public T Resolve(Func<T> factory)
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
                            .Bind<ILifetime<MyClass>>().As(Lifetime.Singleton).To(_ => MyLifetime)
                            .Bind<MyClass>().As(Pure.DI.Lifetime.Binding).To<MyClass>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                    
                }    
            }".Run(out var generatedCode, new RunOptions { Statements = statements});

            // Then
            output.ShouldBe(new[] { "False", "True" }, generatedCode);
        }

        [Fact]
        public void ShouldSupportBindingLifetime()
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

                public class MyLifetime: ILifetime<string>
                {
                    public string Resolve(Func<string> factory)
                    {
                        return factory() + ""_abc"";
                    }
                }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<ILifetime<string>>().To<MyLifetime>()
                            .Bind<string>().As(Pure.DI.Lifetime.Binding).To(_ => ""xyz"")
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                    
                }    
            }".Run(out var generatedCode);

            // Then
            output.ShouldBe(new[] { "xyz_abc" }, generatedCode);
        }
    }
}