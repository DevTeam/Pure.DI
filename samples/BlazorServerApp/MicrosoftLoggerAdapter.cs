// ReSharper disable TemplateIsNotCompileTimeConstantProblem
#pragma warning disable CA2254
namespace BlazorServerApp;

class MicrosoftLoggerAdapter<T>(ILogger<T> logger) : ILog<T>
{
    public void Info(string message) => 
        logger.LogInformation(message);
}