// ReSharper disable SuggestBaseTypeForParameterInConstructor
// ReSharper disable ClassNeverInstantiated.Global

namespace Build.Core;

using Targets;

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
        command.SetHandler(async ctx => {
            Summary($"\"{description}\" started");
            try
            {
                var result = await target.RunAsync(ctx.GetCancellationToken());
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
            command.AddAlias(alias);
        }

        rootCommand.AddCommand(command);
        return Task.CompletedTask;
    }
}