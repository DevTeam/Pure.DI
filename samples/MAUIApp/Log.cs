// ReSharper disable TemplateIsNotCompileTimeConstantProblem
// ReSharper disable ContextualLoggerProblem
namespace MAUIApp;

using Microsoft.Extensions.Logging;

internal class Log<T>: ILog<T>
{
    private readonly ILogger<T> _logger;

    public Log(ILogger<T> logger) =>
        _logger = logger;

    public void Info(string message) => 
        _logger.LogInformation(message);
}