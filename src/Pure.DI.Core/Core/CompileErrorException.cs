namespace Pure.DI.Core;

internal class CompileErrorException(string errorMessage, in Location location, string id)
    : Exception
{
    public string ErrorMessage { get; } = errorMessage;

    public Location Location { get; } = location;

    public string Id { get; } = id;
}