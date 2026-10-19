/*
$v=true
$p=15
$d=Default BCL bindings
$sa=Overriding the BCL binding
$h=Pure.DI provides default bindings for commonly used .NET BCL types, so they can be injected without extra setup code. Ambient values such as `CultureInfo.CurrentCulture`, `TimeProvider.System` and `StringComparer.Ordinal` have an obvious default, and asking for one in a constructor is enough.
$f=>[!NOTE]
$f=>Default BCL bindings can still be overridden in the composition when an application needs a different policy.
$f=
$f=This is what makes time, culture and randomness injectable without a wrapper interface of your own: a test can override `TimeProvider` or `CultureInfo` in one place and every consumer sees the substitute. The defaults read the ambient value, so production behavior stays what the platform provides.
$f=An override has to be declared explicitly; a default binding is still a binding, so two origins for the same type compete and the last applicable one wins. Keep overrides visible near the roots instead of scattering them.
$f=>[!TIP]
$f=>Prefer depending on `TimeProvider` and `CultureInfo` rather than `DateTime.Now` or `CultureInfo.CurrentCulture` directly; the defaults make the same code testable.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.BCL.DefaultBclBindingsScenario;

using System.Globalization;
using System.Security.Cryptography;
using Shouldly;
using Xunit;

// {
//# using Pure.DI;
//# using System.Globalization;
//# using System.Security.Cryptography;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Root<ReportService>("ReportService");

        var composition = new Composition();
        var reportService = composition.ReportService;

        reportService.Culture.ShouldBe(CultureInfo.CurrentCulture);
        reportService.FormatProvider.ShouldBe(reportService.Culture);
        reportService.CompareInfo.ShouldBe(CultureInfo.CurrentCulture.CompareInfo);
        reportService.StringComparer.ShouldBe(StringComparer.Ordinal);
        reportService.StringComparison.ShouldBe(StringComparison.Ordinal);
        reportService.TimeProvider.ShouldBe(TimeProvider.System);
        reportService.Completion.Task.CreationOptions
            .HasFlag(TaskCreationOptions.RunContinuationsAsynchronously)
            .ShouldBeTrue();

        var bytes = new byte[4];
        reportService.RandomNumberGenerator.GetBytes(bytes);
        bytes.Length.ShouldBe(4);
// }
        composition.SaveClassDiagram();
    }
}

// {
class ReportService(
    CultureInfo culture,
    IFormatProvider formatProvider,
    CompareInfo compareInfo,
    StringComparer stringComparer,
    StringComparison stringComparison,
    TimeProvider timeProvider,
    TaskCompletionSource<string> completion,
    RandomNumberGenerator randomNumberGenerator)
{
    public CultureInfo Culture { get; } = culture;

    public IFormatProvider FormatProvider { get; } = formatProvider;

    public CompareInfo CompareInfo { get; } = compareInfo;

    public StringComparer StringComparer { get; } = stringComparer;

    public StringComparison StringComparison { get; } = stringComparison;

    public TimeProvider TimeProvider { get; } = timeProvider;

    public TaskCompletionSource<string> Completion { get; } = completion;

    public RandomNumberGenerator RandomNumberGenerator { get; } = randomNumberGenerator;
}
// }
