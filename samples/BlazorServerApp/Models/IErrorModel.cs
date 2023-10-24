namespace BlazorServerApp.Models;

public interface IErrorModel
{
    string? RequestId { get; }
    
    bool ShowRequestId { get; }
}