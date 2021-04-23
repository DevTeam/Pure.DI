namespace Clock.Models
{
    using System;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class SystemClock : IClock
    {
        public DateTimeOffset Now => DateTimeOffset.Now;
    }
}