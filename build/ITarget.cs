namespace Build;

using System.CommandLine.Invocation;

internal interface ITarget<T>
{
    Task<T> RunAsync(InvocationContext ctx);
}