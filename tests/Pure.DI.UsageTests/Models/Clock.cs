// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.UsageTests.Models;

internal class Clock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}