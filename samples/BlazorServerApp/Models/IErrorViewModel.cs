namespace BlazorServerApp.Models;

public interface IErrorViewModel
{
    string? RequestId { get; }
    
    bool ShowRequestId { get; }
}