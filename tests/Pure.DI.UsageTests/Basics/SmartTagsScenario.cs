/*
$v=true
$p=7
$d=Smart tags
$h=When you have a large graph of objects, you may need a lot of tags to neatly define all the dependencies in it. Strings or other constant values are not always convenient to use, because they have too much variability. And there are often cases when you specify one tag in the binding, but the same tag in the dependency, but with a typo, which leads to a compilation error when checking the dependency graph. The solution to this problem is to create an `Enum` type and use its values as tags. Pure.DI makes it easier to solve this problem.
$h=
$h=When you specify a tag in a binding and the compiler can't determine what that value is, Pure.DI will automatically create a constant for it inside the `Pure.DI.Tag` type. For the example below, the set of constants would look like this:
$h=
$h=```c#
$h=namespace Pure.DI
$h={
$h=  internal partial class Tag
$h=  {
$h=    public const string Abc = "Abc";
$h=    public const string Xyz = "Xyz";
$h=  }
$h=}
$h=```
$h=So you can apply refactoring in the development environment. And also tag changes in bindings will be automatically checked by the compiler. This will reduce the number of errors.
$h=
$h=![](smart_tags.gif)
$h=
$h=The example below also uses the `using static Pure.DI.Tag;` directive to access tags in `Pure.DI.Tag` without specifying a type name:
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable PreferConcreteValueOverDefault
// ReSharper disable RedundantNameQualifier
namespace Pure.DI.UsageTests.Basics.SmartTagsScenario;

using Shouldly;
using static Pure.DI.Tag;
using static Pure.DI.Lifetime;
using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        //# using static Pure.DI.Tag;
        //# using static Pure.DI.Lifetime;

        DI.Setup(nameof(Composition))
            // The `default` tag is used to resolve dependencies
            // when the tag was not specified by the consumer
            .Bind<IMessageSender>(Email, default).To<EmailSender>()
            .Bind<IMessageSender>(Sms).As(Singleton).To<SmsSender>()
            .Bind<IMessagingService>().To<MessagingService>()

            // "SmsSenderRoot" is root name, Sms is tag
            .Root<IMessageSender>("SmsSenderRoot", Sms)

            // Specifies to create the composition root named "Root"
            .Root<IMessagingService>("MessagingService");

        var composition = new Composition();
        var messagingService = composition.MessagingService;
        messagingService.EmailSender.ShouldBeOfType<EmailSender>();
        messagingService.SmsSender.ShouldBeOfType<SmsSender>();
        messagingService.SmsSender.ShouldBe(composition.SmsSenderRoot);
        messagingService.DefaultSender.ShouldBeOfType<EmailSender>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IMessageSender;

class EmailSender : IMessageSender;

class SmsSender : IMessageSender;

interface IMessagingService
{
    IMessageSender EmailSender { get; }

    IMessageSender SmsSender { get; }

    IMessageSender DefaultSender { get; }
}

class MessagingService(
    [Tag(Email)] IMessageSender emailSender,
    [Tag(Sms)] IMessageSender smsSender,
    IMessageSender defaultSender)
    : IMessagingService
{
    public IMessageSender EmailSender { get; } = emailSender;

    public IMessageSender SmsSender { get; } = smsSender;

    public IMessageSender DefaultSender { get; } = defaultSender;
}
// }