// ReSharper disable TemplateIsNotCompileTimeConstantProblem
// ReSharper disable ContextualLoggerProblem
namespace BlazorWebAssemblyApp;

using System.Diagnostics.CodeAnalysis;
using Clock.Models;

class Log<T>(ILogger<T> logger) : ILog<T>
{
    [SuppressMessage("Usage", "CA2254:Template should be a static expression")]
    public void Info(string message) =>
        logger.LogInformation(message);
}