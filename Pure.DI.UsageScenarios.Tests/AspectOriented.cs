// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable CA2211
// ReSharper disable MemberCanBePrivate.Global
namespace Pure.DI.UsageScenarios.Tests
{
    using System;
    using Moq;
    using Xunit;

    public class AspectOriented
    {
        public static Mock<IConsole> Console = new();

        public AspectOriented() => Console = new Mock<IConsole>();

        [Fact]
        // $visible=true
        // $tag=1 Basics
        // $priority=02
        // $description=Aspect-oriented DI
        // $header=This framework has no special predefined attributes to support aspect-oriented auto wiring because a non-infrastructure code should not have references to this framework. But this code may contain these attributes by itself. And it is quite easy to use these attributes for aspect-oriented auto wiring, see the sample below.
        // $footer=You can also specify your own aspect-oriented autowiring by implementing the interface [_IAutowiringStrategy_](IoCContainer/blob/master/IoC/IAutowiringStrategy.cs).
        // {
        public void Run()
        {
            DI.Setup()
                // Define attributes for aspect-oriented autowiring
                .TypeAttribute<TypeAttribute>()
                .OrderAttribute<OrderAttribute>()
                .TagAttribute<TagAttribute>()
                // Configure the container to use DI aspects
                .Bind<IConsole>().Tag("MyConsole").To(_ => AspectOriented.Console.Object)
                .Bind<string>().Tag("Prefix").To(_ => "info")
                .Bind<ILogger>().To<Logger>();

            // Create a logger
            var logger = AspectOrientedDI.Resolve<ILogger>();

            // Log the message
            logger.Log("Hello");

            // Check the output has the appropriate format
            Console.Verify(i => i.WriteLine(It.IsRegex(".+ - info: Hello")));
        }

        // Represents the dependency aspect attribute to specify a type for injection.
        [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method)]
        public class TypeAttribute : Attribute
        {
            // A type, which will be used during an injection
            public readonly Type Type;

            public TypeAttribute(Type type) => Type = type;
        }

        // Represents the dependency aspect attribute to specify a tag for injection.
        [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method | AttributeTargets.Property)]
        public class TagAttribute : Attribute
        {
            // A tag, which will be used during an injection
            public readonly object Tag;

            public TagAttribute(object tag) => Tag = tag;
        }

        // Represents the dependency aspect attribute to specify an order for injection.
        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
        public class OrderAttribute : Attribute
        {
            // An order to be used to invoke a method
            public readonly int Order;

            public OrderAttribute(int order) => Order = order;
        }

        public interface IConsole { void WriteLine(string text); }

        public interface IClock { DateTimeOffset Now { get; } }

        public interface ILogger { void Log(string message); }

        public class Logger : ILogger
        {
            private readonly IConsole _console;
            private IClock? _clock;

            // Constructor injection using the tag "MyConsole"
            public Logger([Tag("MyConsole")] IConsole console) => _console = console;

            // Method injection after constructor using specified type _Clock_
            [Order(1)] public void Initialize([Type(typeof(Clock))] IClock clock) => _clock = clock;

            // Setter injection after the method injection above using the tag "Prefix"
            [Tag("Prefix"), Order(2)]
            public string Prefix { get; set; } = string.Empty;

            // Adds current time and prefix before a message and writes it to console
            public void Log(string message) => _console?.WriteLine($"{_clock?.Now} - {Prefix}: {message}");
        }

        public class Clock : IClock
        {
            // "clockName" dependency is not resolved here but has default value
            public Clock([Type(typeof(string)), Tag("ClockName")] string clockName = "SPb") { }

            public DateTimeOffset Now => DateTimeOffset.Now;
        }
        // }
    }
}
