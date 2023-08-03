namespace Pure.DI.Core;

internal class CompileErrorException: Exception
{
    public CompileErrorException(string errorMessage, in Location location, string id)
    {
        ErrorMessage = errorMessage;
        Location = location;
        Id = id;
    }

    public string ErrorMessage { get; }
    
    public Location Location { get; }
    
    public string Id { get; }
}