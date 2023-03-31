namespace Build;

using System.CommandLine.Invocation;

internal interface ITarget
{
    Task RunAsync(InvocationContext ctx);
}