// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedVariable
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeNamespaceBody
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
                .Bind<TT>().To(ctx =>
                {
                    // Put any logic here to create an instance of the TT type
                    // For example, some IoC container can be used to obtain an instance.
                    if (typeof(TT) == typeof(int))
                    {
                        return (TT)(object)33;
                    }
                    
                    if (typeof(TT) == typeof(string))
                    {
                        return (TT)(object)"Abc";
                    }

                    throw new Exception("Unknown type.");
                })
                .Bind<Consumer>().To<Consumer>();
            
            var instance = DefaultFactoryDI.Resolve<Consumer>();
            instance.Value.ShouldBe(33);
            instance.Text.ShouldBe("Abc");
        }

        public record Consumer(int Value, string Text);
        // }
    }
}