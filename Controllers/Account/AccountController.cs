using Microsoft.AspNetCore.Mvc;
using BilliardsManagement.Models;
using BilliardsManagement.Helpers;
using Microsoft.AspNetCore.Http;
using System.Linq;
using BilliardsManagement.Models.Entities;
using BilliardsManagement.Models.ViewModels;

namespace BilliardsManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly BilliardsDbContext _context;

        public AccountController(BilliardsDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Clear any existing session
            HttpContext.Session.Clear();
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View(model);
            }

            if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
            {
                ViewBag.Error = "Vui lòng nhập tên đăng nhập và mật khẩu.";
                return View(model);
            }

            try
            {
                string hashedPassword = PasswordHasher.ComputeSha256Hash(model.Password);

                var employee = _context.Employees
                    .FirstOrDefault(e => e.Username == model.Username && e.Password == hashedPassword);

                if (employee == null)
                {
                    ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng.";
                    return View(model);
                }

                // Set session values
                HttpContext.Session.SetInt32("EmployeeId", employee.EmployeeId);
                HttpContext.Session.SetString("FullName", employee.FullName ?? "");
                HttpContext.Session.SetString("Role", employee.Position ?? "");

                // Redirect based on role
                switch (employee.Position?.ToUpper())
                {
                    case "MANAGER":
                        return RedirectToAction("Index", "Manager");
                    case "CASHIER":
                        return RedirectToAction("Index", "Cashier");
                    case "SERVING":
                        return RedirectToAction("Index", "Serving");
                    default:
                        ViewBag.Error = "Không xác định được vai trò người dùng.";
                        return View(model);
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Có lỗi xảy ra trong quá trình đăng nhập. Vui lòng thử lại sau.";
                return View(model);
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if username already exists
            if (_context.Employees.Any(e => e.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
                return View(model);
            }

            // Check if email already exists
            if (_context.Employees.Any(e => e.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng.");
                return View(model);
            }

            var employee = new Employee
            {
                Username = model.Username,
                Password = PasswordHasher.ComputeSha256Hash(model.Password),
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                Position = "SERVING" // Default role
            };

            _context.Employees.Add(employee);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction(nameof(Login));
        }
    }
}
