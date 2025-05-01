namespace MAUIReactorApp;

using Microsoft.Extensions.Logging;

public interface IApiHelper;

public class ApiHelper(ILogger<ApiHelper> logger, IHttpClientFactory httpClientFactory)
    : IApiHelper, IDisposable
{
    private readonly ILogger<ApiHelper> _logger = logger;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("ApiClient");

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

public interface IService;

public class MyService(IApiHelper apiHelper) : IService
{
    private readonly IApiHelper _apiHelper = apiHelper;
}