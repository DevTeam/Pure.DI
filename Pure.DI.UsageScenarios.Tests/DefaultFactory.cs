// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedVariable
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable SuspiciousTypeConversion.Global
namespace Pure.DI.UsageScenarios.Tests
{
    using Xunit;
    
    public class DefaultFactory
    {
        [Fact]
        // $visible=true
        // $tag=1 Basics
        // $priority=99
        // $description=Default factory
        // $header=Sometimes it is necessary to add custom dependency resolution logic for types that do not have any bindings defined. In this case, you can only use factory binding for the generic type marker and implement your own dependency resolution logic, as in the example below:
        // {
        public void Run()
        {
            DI.Setup()
                // For any types
                .Bind<TT>().To(ctx =>
                {
                    // Put any logic here to create an instance of any type
                    // For example, some IoC container can be used to obtain an instance.
                    if (typeof(TT) == typeof(int))
                    {
                        return (TT)(object)99;
                    }
                    
                    if (typeof(TT) == typeof(string))
                    {
                        return (TT)(object)"Abc";
                    }

                    throw new Exception("Unknown type.");
                })
                .Bind<TTS>().To(ctx =>
                {
                    // This factory will be chosen for "int" value resolution
                    // because TTS (as well as TTS1, TTS2 ...) is a value type marker that is more suitable for "int" value type resolution.
                    if (typeof(TTS) == typeof(int))
                    {
                        return (TTS)(object)33;
                    }
                    
                    throw new Exception("Unknown type.");
                })
                .Bind<TTDisposable>().To(ctx =>
                {
                    // This factory will be chosen for IDisposable resolution
                    // because TTDisposable (as well as TTDisposable1, TTDisposable2 ...) is a IDisposable type marker that is more suitable for IDisposable type resolution.
                    if (typeof(TTDisposable) == typeof(IDisposable))
                    {
                        return (TTDisposable)new Service(new Dependency());
                    }
                    
                    throw new Exception("Unknown type.");
                })
                .Bind<Consumer>().To<Consumer>();
            
            var instance = DefaultFactoryDI.Resolve<Consumer>();
            instance.Value.ShouldBe(33);
            instance.Text.ShouldBe("Abc");
            instance.Disposable.ShouldBeOfType<Service>();
        }

        public record Consumer(int Value, string Text, IDisposable Disposable);
        // }
    }
}