namespace MAUIReactorApp;

using Microsoft.Extensions.Logging;

internal class MicrosoftLoggerAdapter<T>(ILogger<T> logger) : ILog<T>
{
    public void Info(string message) => 
        logger.LogInformation(message);
}