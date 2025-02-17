namespace GrpcService.Services;

using Grpc.Core;

public class GreeterService(ILogger<GreeterService> logger) : Greeter.GreeterBase
{
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        logger.LogInformation("Hello");
        return Task.FromResult(new HelloReply
        {
            Message = "Hello " + request.Name
        });
    }
}