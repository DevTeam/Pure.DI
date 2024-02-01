namespace Build.Tools;

internal interface ICommands
{
    ValueTask Register<T>(
        ITarget<T> target,
        string description,
        string name,
        params string[] aliases);
}