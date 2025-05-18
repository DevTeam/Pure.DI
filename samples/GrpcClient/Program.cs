using var composition = new Composition();
var root = composition.Root;

await root.Run();

partial class Program(
    IConsole console,
    ClockGrpcService.ClockGrpcServiceClient client)
{
    private async Task Run()
    {
        while (!console.KeyAvailable)
        {
            var reply = await client.GetNowAsync(new NowRequest());
            console.Write($"{reply.Date} {reply.Time}");
        }
    }
}