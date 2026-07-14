using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Mvc.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Error()
    {
        return View();
    }
}
