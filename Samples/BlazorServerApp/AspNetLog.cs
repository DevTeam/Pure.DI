// ReSharper disable ClassNeverInstantiated.Global
namespace BlazorServerApp
{
    using Clock.Models;
    using Microsoft.Extensions.Logging;

    internal class AspNetLog<T>: ILog<T>
    {
        private readonly ILogger<T> _logger;

        public AspNetLog(ILogger<T> logger) => _logger = logger;

        public void Info(string message) => _logger.Log(LogLevel.Information, message);
    }
}
