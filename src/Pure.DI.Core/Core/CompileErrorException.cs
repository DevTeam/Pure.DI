namespace Pure.DI.Core;

sealed class CompileErrorException(string errorMessage, in ImmutableArray<Location> locations, string id)
    : Exception
{
    public string ErrorMessage { get; } = errorMessage;

    public ImmutableArray<Location> Locations { get; } = locations;

    public string Id { get; } = id;
}