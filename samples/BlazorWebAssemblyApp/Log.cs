// ReSharper disable TemplateIsNotCompileTimeConstantProblem
// ReSharper disable ContextualLoggerProblem
namespace BlazorWebAssemblyApp;

using Clock.Models;
using Microsoft.Extensions.Logging;

internal class Log<T>(ILogger<T> logger) : ILog<T>
{
    public void Info(string message) => 
        logger.LogInformation(message);
}