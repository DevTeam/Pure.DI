// ReSharper disable UnusedTypeParameter
namespace Clock.Models;

public interface ILog<T>
{
    void Info(string message);
}