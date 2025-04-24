using Microsoft.AspNetCore.Mvc;


public class CashierController : Controller {


    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}