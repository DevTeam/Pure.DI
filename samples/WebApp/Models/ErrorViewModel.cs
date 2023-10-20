namespace WebApp.Models;

public record ErrorViewModel(string RequestId = "")
{
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}