// ReSharper disable SuggestBaseTypeForParameterInConstructor
// ReSharper disable ClassNeverInstantiated.Global
namespace Build.Tools;

[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
internal class Commands(RootCommand rootCommand)
{
    public Task Register<T>(
        ITarget<T> target,
        string description,
        string name,
        params string[] aliases)
    {
        var command = new Command(name, description);
        command.SetHandler(ctx =>
        {
            WriteLine($"---------- {description} ----------", Color.Highlighted);
            return target.RunAsync(ctx.GetCancellationToken());
        });
        
        foreach (var alias in aliases)
        {
            command.AddAlias(alias);   
        }
        
        rootCommand.AddCommand(command);
        return Task.CompletedTask;
    }
}