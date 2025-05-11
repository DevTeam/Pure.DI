namespace GrpcService;

public class ClockService(IAppViewModel app, IClockViewModel clock)
    : ClockGrpcService.ClockGrpcServiceBase
{
    public override Task<NowReply> GetNow(NowRequest request, ServerCallContext context) =>
        Task.FromResult(new NowReply
        {
            Title = app.Title,
            Date = clock.Date,
            Time = clock.Time
        });
}