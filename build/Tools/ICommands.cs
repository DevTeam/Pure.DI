namespace Build.Tools;

internal interface ICommands
{
    Task Register<T>(ITarget<T> target,
        string description,
        string name,
        params string[] aliases);
}