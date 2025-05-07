using Microsoft.AspNetCore.Mvc;

namespace BilliardsManagement.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            // If user is already logged in, redirect to their dashboard
            var userRole = HttpContext.Session.GetString("Role")?.ToUpper();
            if (!string.IsNullOrEmpty(userRole))
            {
                return userRole switch
                {
                    "MANAGER" => RedirectToAction("Index", "Manager"),
                    "CASHIER" => RedirectToAction("Index", "Cashier"),
                    "SERVING" => RedirectToAction("Index", "Serving"),
                    _ => View()
                };
            }
            return View();
        }
    }
}