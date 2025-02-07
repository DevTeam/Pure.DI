﻿// ReSharper disable SuggestBaseTypeForParameterInConstructor
// ReSharper disable ClassNeverInstantiated.Global

namespace Build.Tools;

[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
internal class Commands(RootCommand rootCommand)
{
    public Task RegisterAsync<T>(
        ITarget<T> target,
        string description,
        string name,
        params string[] aliases)
    {
        var command = new Command(name, description);
        command.SetHandler(async ctx =>
        {
            Summary($"\"{description}\" started");
            try
            {
                var result = await target.RunAsync(ctx.GetCancellationToken());
                Summary($"\"{description}\" finished with result: {result}");
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