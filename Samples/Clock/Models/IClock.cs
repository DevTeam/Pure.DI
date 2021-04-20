namespace Clock.Models
{
    using System;

    internal interface IClock
    {
        DateTimeOffset Now { get; }
    }
}
