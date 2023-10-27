// ReSharper disable NotAccessedField.Local
namespace BlazorServerApp.Models;

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorViewModel : PageModel, IErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    private readonly ILogger<ErrorViewModel> _logger;

    public ErrorViewModel(ILogger<ErrorViewModel> logger) => 
        _logger = logger;

    public void OnGet() => 
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
}