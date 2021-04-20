namespace Clock.Models
{
    using System;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class SimpleClock : IClock
    {
        public DateTimeOffset Now => DateTimeOffset.Now;
    }
}