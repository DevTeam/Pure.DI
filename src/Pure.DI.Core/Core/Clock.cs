// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class Clock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}