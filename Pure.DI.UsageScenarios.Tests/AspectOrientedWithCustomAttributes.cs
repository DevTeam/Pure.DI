// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable CA2211
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CA2211
// ReSharper disable ArrangeStaticMemberQualifier
// ReSharper disable NotAccessedField.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ArrangeNamespaceBody
namespace Pure.DI.UsageScenarios.Tests
{
    using System;
    using Moq;
    using Xunit;
    using static Lifetime;

    public class AspectOrientedWithCustomAttributes
    {
#pragma warning disable CA2211
        public static Mock<IConsole> Console = new();
#pragma warning restore CA2211

        public AspectOrientedWithCustomAttributes() => Console = new Mock<IConsole>();

        [Fact]
        // $visible=true
        // $tag=1 Basics
        // $priority=03
        // $description=Aspect-oriented DI with custom attributes
        // $header=There is already a set of predefined attributes to support aspect-oriented autowiring such as _TypeAttribute_. But in addition, you can use your own attributes, see the sample below.
        // {
        public void Run()
        {
            DI.Setup()
                // Define custom attributes for aspect-oriented autowiring
                .TypeAttribute<MyTypeAttribute>()
                .OrderAttribute<MyOrderAttribute>()
                .TagAttribute<MyTagAttribute>()
                // Starting C# 11 you can use a generic attributes
                .TypeAttribute<MyTypeAttribute<TT>>()

                .Bind<IConsole>().Tags("MyConsole").To(_ => AspectOrientedWithCustomAttributes.Console.Object)
                .Bind<string>().Tags("Prefix").To(_ => "info")
                .Bind<ILogger>().As(Singleton).To<Logger>();

            // Create a logger
            var logger = AspectOrientedWithCustomAttributesDI.Resolve<ILogger>();

            // Log the message
            logger.Log("Hello");

            // Check the output has the appropriate format
            Console.Verify(i => i.WriteLine(It.IsRegex(".+ - info: Hello")));
        }

        // Represents the dependency aspect attribute to specify a type for injection.
        [AttributeUsage(
            AttributeTargets.Parameter
            | AttributeTargets.Property
            | AttributeTargets.Field)]
        public class MyTypeAttribute : Attribute
        {
            public MyTypeAttribute(Type type) { }
        }
        
        // Starting C# 11 you can use a generic attributes
        [AttributeUsage(
            AttributeTargets.Parameter
            | AttributeTargets.Property
            | AttributeTargets.Field)]
        public class MyTypeAttribute<T> : Attribute
        {
        }

        // Represents the dependency aspect attribute to specify a tag for injection.
        [AttributeUsage(
            AttributeTargets.Parameter
            | AttributeTargets.Property
            | AttributeTargets.Field)]
        public class MyTagAttribute : Attribute
        {
            public MyTagAttribute(object tag) { }
        }

        // Represents the dependency aspect attribute to specify an order for injection.
        [AttributeUsage(
            AttributeTargets.Constructor
            | AttributeTargets.Method
            | AttributeTargets.Property
            | AttributeTargets.Field)]
        public class MyOrderAttribute : Attribute
        {
            public MyOrderAttribute(int order) { }
        }

        public interface IConsole { void WriteLine(string text); }

        public interface IClock { DateTimeOffset Now { get; } }

        public interface ILogger { void Log(string message); }

        public class Logger : ILogger
        {
            private readonly IConsole _console;
            private IClock? _clock;

            // Constructor injection using the tag "MyConsole"
            public Logger([MyTag("MyConsole")] IConsole console) => _console = console;

            // Method injection after constructor using specified type _Clock_
            [MyOrder(1)] public void Initialize([MyType<Clock>] IClock clock) => _clock = clock;

            // Setter injection after the method injection above using the tag "Prefix"
            [MyTag("Prefix"), MyOrder(2)]
            public string Prefix { get; set; } = string.Empty;

            // Adds current time and prefix before a message and writes it to console
            public void Log(string message) => _console.WriteLine($"{_clock?.Now} - {Prefix}: {message}");
        }

        public class Clock : IClock
        {
            // "clockName" dependency is not resolved here but has default value
            public Clock([MyType(typeof(string)), MyTag("ClockName")] string clockName = "SPb") { }

            public DateTimeOffset Now => DateTimeOffset.Now;
        }
        // }
    }
}
