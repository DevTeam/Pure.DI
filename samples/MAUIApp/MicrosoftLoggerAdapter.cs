namespace MAUIApp;

internal class MicrosoftLoggerAdapter<T>(ILogger<T> logger) : ILog<T>
{
    public void Info(string message) => 
        logger.LogInformation(message);
}