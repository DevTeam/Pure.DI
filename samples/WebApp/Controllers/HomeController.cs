using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
// ReSharper disable WithExpressionModifiesAllMembers

namespace WebApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IndexViewModel _indexViewModel;
    private readonly PrivacyViewModel _privacyViewModel;
    private readonly ErrorViewModel _errorViewModel;

    public HomeController(
        ILogger<HomeController> logger,
        IndexViewModel indexViewModel,
        PrivacyViewModel privacyViewModel,
        ErrorViewModel errorViewModel)
    {
        _logger = logger;
        _indexViewModel = indexViewModel;
        _privacyViewModel = privacyViewModel;
        _errorViewModel = errorViewModel;
    }

    public IActionResult Index()
    {
        _logger.LogInformation("Show Index");
        return View(_indexViewModel);
    }

    public IActionResult Privacy()
    {
        _logger.LogInformation("Show Privacy");
        return View(_privacyViewModel);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        _logger.LogInformation("Show error");
        return View(_errorViewModel with { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}