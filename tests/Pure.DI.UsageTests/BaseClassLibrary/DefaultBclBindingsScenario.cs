/*
$v=true
$p=15
$d=Default BCL bindings
$sa=Overriding the BCL binding
$h=Pure.DI provides default bindings for commonly used .NET BCL types, so they can be injected without extra setup code.
$f=>[!NOTE]
$f=>Default BCL bindings can still be overridden in the composition when an application needs a different policy.
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
