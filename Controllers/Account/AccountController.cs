using Microsoft.AspNetCore.Mvc;
using BilliardsManagement.Models;
using BilliardsManagement.Helpers;
using Microsoft.AspNetCore.Http;
using System.Linq;
using BilliardsManagement.Models.Entities;
using BilliardsManagement.Models.ViewModels;
using BilliardsManagement.Repositories;

namespace BilliardsManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;

        public AccountController(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Clear any existing session
            HttpContext.Session.Clear();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
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

                var employee = await _employeeRepository.GetByUsernameAndPasswordAsync(model.Username, hashedPassword);

                if (employee == null)
                {
                    ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng.";
                    return View(model);
                }

                // Set session values
                HttpContext.Session.SetInt32("EmployeeId", employee.EmployeeId);
                HttpContext.Session.SetString("FullName", employee.FullName ?? "");
                HttpContext.Session.SetString("Role", employee.Position ?? "");
                HttpContext.Session.SetString("Username", employee.Username ?? "");

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
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if username already exists
            if (await _employeeRepository.IsUsernameExistsAsync(model.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
                return View(model);
            }

            // Check if email already exists
            if (await _employeeRepository.IsEmailExistsAsync(model.Email))
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
                Position = "SERVING", // Default role
                CreatedAt = DateTime.Now
            };

            await _employeeRepository.CreateAsync(employee);

            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction(nameof(Login));
        }
    }
}
