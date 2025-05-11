namespace WebAPI.Controllers;

[Route("[controller]")]
public class ClockController(
    ILogger<ClockController> logger,
    IAppViewModel app,
    IClockViewModel clock) : ControllerBase
{
    [HttpGet]
    public Task<ClockResult> GetDateTime()
    {
        logger.LogInformation(nameof(GetDateTime));
        return Task.FromResult(new ClockResult(app.Title, clock.Date, clock.Time));
    }
}