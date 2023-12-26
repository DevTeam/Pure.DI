using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
// ReSharper disable WithExpressionModifiesAllMembers

namespace WebApp.Controllers;

public class HomeController(
    ILogger<HomeController> logger,
    IndexViewModel indexViewModel,
    PrivacyViewModel privacyViewModel,
    ErrorViewModel errorViewModel)
    : Controller
{
    public IActionResult Index()
    {
        logger.LogInformation("Show Index");
        return View(indexViewModel);
    }

    public IActionResult Privacy()
    {
        logger.LogInformation("Show Privacy");
        return View(privacyViewModel);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        logger.LogInformation("Show error");
        return View(errorViewModel with { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}