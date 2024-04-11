var composition = new $(CompositionName)(args);
return await composition.Root.RunAsync();

internal partial class Program(
    string[] args,
    IInput input,
    IOutput output)
{
    private async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        await output.WriteLineAsync("Hello!", cancellationToken);
        await output.WriteLineAsync(string.Join(", ", args), cancellationToken);

        await output.WriteLineAsync("Press the Enter key to exit.", cancellationToken);
        await input.ReadLineAsync(cancellationToken);

        return 0;
    }
}