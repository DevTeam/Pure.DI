// ReSharper disable TemplateIsNotCompileTimeConstantProblem
// ReSharper disable ContextualLoggerProblem
#pragma warning disable CA2254
namespace GrpcService;

class MicrosoftLoggerAdapter<T>(ILogger<T> logger) : ILog<T>
{
    public void Info(string message) => 
        logger.LogInformation(message);
}