using Microsoft.AspNetCore.Mvc;

public class ManagerController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}