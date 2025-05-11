namespace Clock;

public sealed class DebugLog<T>(IClockModel clockModel) : ILog<T>
{
    public void Info(string message) =>
        Debug.WriteLine($"{clockModel.Now:HH:mm:ss.fff} {typeof(T).Name,-32} {message}");
}