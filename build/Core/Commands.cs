// ReSharper disable SuggestBaseTypeForParameterInConstructor
// ReSharper disable ClassNeverInstantiated.Global

namespace Build.Core;

[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
class Commands(RootCommand rootCommand)
{
    public Task RegisterAsync<T>(
        ITarget<T> target,
        string description,
        string name,
        params string[] aliases)
    {
        var command = new Command(name, description);
        command.SetAction(async _ => {
            Summary($"\"{description}\" started");
            try
            {
                var result = await target.RunAsync(CancellationToken.None);
                Summary($"\"{description}\" ", "finished".WithColor(Color.Success), " with result ", result.WithColor(Color.Details));
            }
            catch (Exception)
            {
                Error($"\"{description}\" failed");
                throw;
            }
        });

        foreach (var alias in aliases)
        {
            command.Aliases.Add(alias);
        }

        rootCommand.Subcommands.Add(command);
        return Task.CompletedTask;
    }
}