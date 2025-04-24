using Microsoft.AspNetCore.Mvc;

public class ServingController : Controller {


    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}