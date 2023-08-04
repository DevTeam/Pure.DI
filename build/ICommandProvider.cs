namespace Build;

using System.CommandLine;

internal interface ICommandProvider
{
    Command Command { get; }
}