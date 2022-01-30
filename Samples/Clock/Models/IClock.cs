namespace Clock.Models
{
    internal interface IClock
    {
        DateTimeOffset Now { get; }
    }
}