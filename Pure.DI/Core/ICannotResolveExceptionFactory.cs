namespace Pure.DI.Core
{
    internal interface ICannotResolveExceptionFactory
    {
        HandledException Create(BindingMetadata binding, string description);
    }
}