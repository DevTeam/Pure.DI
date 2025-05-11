namespace WebApp.Controllers;

public class ClockController(IClockViewModel clock)
    : Controller
{
    public IActionResult Clock()
    {
        Response.Headers.Append("Refresh", "1");
        return View(clock);
    }
}