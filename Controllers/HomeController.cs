using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskManagerSystem.Models;

namespace TaskManagerSystem.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    // Constructor: Logger dependency injection ile alýnýr
    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    // GET: Ana sayfa
    public IActionResult Index()
    {
        return View();
    }

    // GET: Gizlilik politikasý sayfasý
    public IActionResult Privacy()
    {
        return View();
    }

    // Hata sayfasý
    // ResponseCache ile sayfanýn cache'lenmemesi saðlanýr
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        // ErrorViewModel ile RequestId bilgisi gönderilir
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // Kullanýcý çýkýþ iþlemi
    // Sadece login sayfasýna yönlendirir
    public IActionResult Logout()
    {
        return RedirectToAction("Login", "Account");
    }
}
