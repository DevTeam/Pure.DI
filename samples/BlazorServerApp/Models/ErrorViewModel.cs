// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedMember.Local
namespace BlazorServerApp.Models;

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorViewModel(ILogger<ErrorViewModel> logger)
    : PageModel, IErrorViewModel
{
    private readonly ILogger<ErrorViewModel> _logger = logger;

    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public void OnGet() =>
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
}