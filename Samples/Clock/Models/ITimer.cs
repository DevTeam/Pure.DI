namespace Clock.Models
{
    using System;

    internal interface ITimer: IObservable<Tick> { }
}
