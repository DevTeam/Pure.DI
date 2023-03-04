namespace Pure.DI.Core;

internal interface IClock
{
    DateTimeOffset Now { get; }
}