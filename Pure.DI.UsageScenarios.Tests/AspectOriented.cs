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
namespace Pure.DI.UsageScenarios.Tests;

using Moq;
using static Lifetime;

public class AspectOriented
{
#pragma warning disable CA2211
    public static Mock<IConsole> Console = new();
#pragma warning restore CA2211

    public AspectOriented() => Console = new Mock<IConsole>();

    [Fact]
    // $visible=true
    // $tag=1 Basics
    // $priority=02
    // $description=Aspect-oriented DI
    // {
    public void Run()
    {
        DI.Setup()
            .Bind<IConsole>().Tags("MyConsole").To(_ => AspectOriented.Console.Object)
            .Bind<string>().Tags("Prefix").To(_ => "info")
            .Bind<ILogger>().As(Singleton).To<Logger>();

        // Create a logger
        var logger = AspectOrientedDI.Resolve<ILogger>();

        // Log the message
        logger.Log("Hello");

        // Check the output has the appropriate format
        Console.Verify(i => i.WriteLine(It.IsRegex(".+ - info: Hello")));
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
        public void Log(string message) => _console.WriteLine($"{_clock?.Now} - {Prefix}: {message}");
    }

    public class Clock : IClock
    {
        // "clockName" dependency is not resolved here but has default value
        public Clock([Type(typeof(string)), Tag("ClockName")] string clockName = "SPb") { }

        public DateTimeOffset Now => DateTimeOffset.Now;
    }
    // }
}