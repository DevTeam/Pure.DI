// ReSharper disable TemplateIsNotCompileTimeConstantProblem
// ReSharper disable ContextualLoggerProblem
namespace MAUIApp;

using Microsoft.Extensions.Logging;

class Log<T>(ILogger<T> logger) : ILog<T>
{
    public void Info(string message) => 
        logger.LogInformation(message);
}