namespace Clock.Models;

interface IClock
{
    DateTimeOffset Now { get; }
}