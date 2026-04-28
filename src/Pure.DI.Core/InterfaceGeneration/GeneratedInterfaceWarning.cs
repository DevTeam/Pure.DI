namespace Pure.DI.InterfaceGeneration;

sealed class GeneratedInterfaceWarning(
    string id,
    string message,
    string messageKey,
    Location location)
{
    public string Id { get; } = id;

    public string Message { get; } = message;

    public string MessageKey { get; } = messageKey;

    public Location Location { get; } = location;
}
