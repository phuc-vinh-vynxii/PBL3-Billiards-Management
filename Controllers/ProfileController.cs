using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;
using BilliardsManagement.Models.ViewModels;
using BilliardsManagement.Repositories;
using BilliardsManagement.Helpers;

namespace BilliardsManagement.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;

        public ProfileController(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Check if user is logged in
            var employeeId = HttpContext.Session.GetInt32("EmployeeId");
            var role = HttpContext.Session.GetString("Role");

            if (!employeeId.HasValue || string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _employeeRepository.GetByIdAsync(employeeId.Value);
            if (employee == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new ProfileViewModel
            {
                EmployeeId = employee.EmployeeId,
                FullName = employee.FullName,
                Position = employee.Position,
                Phone = employee.Phone,
                Email = employee.Email,
                Username = employee.Username,
                CreatedAt = employee.CreatedAt
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            var employeeId = HttpContext.Session.GetInt32("EmployeeId");
            if (!employeeId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var employee = await _employeeRepository.GetByIdAsync(employeeId.Value);
            if (employee == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if email is being changed and already exists
            if (employee.Email != model.Email)
            {
                var emailExists = await _employeeRepository.IsEmailExistsAsync(model.Email, employeeId.Value);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Email đã được sử dụng bởi tài khoản khác");
                    return View("Index", model);
                }
            }

            // Update employee information
            employee.FullName = model.FullName;
            employee.Phone = model.Phone;
            employee.Email = model.Email;

            await _employeeRepository.UpdateAsync(employee);

            // Update session if FullName changed
            HttpContext.Session.SetString("FullName", employee.FullName ?? "");

            TempData["Success"] = "Cập nhật thông tin thành công";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var employeeId = HttpContext.Session.GetInt32("EmployeeId");
            if (!employeeId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin";
                return RedirectToAction("Index");
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Mật khẩu mới và xác nhận mật khẩu không khớp";
                return RedirectToAction("Index");
            }

            if (newPassword.Length < 6)
            {
                TempData["Error"] = "Mật khẩu mới phải có ít nhất 6 ký tự";
                return RedirectToAction("Index");
            }

            var employee = await _employeeRepository.GetByIdAsync(employeeId.Value);
            if (employee == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Verify current password
            string hashedCurrentPassword = PasswordHasher.ComputeSha256Hash(currentPassword);
            if (employee.Password != hashedCurrentPassword)
            {
                TempData["Error"] = "Mật khẩu hiện tại không đúng";
                return RedirectToAction("Index");
            }

            // Update password
            employee.Password = PasswordHasher.ComputeSha256Hash(newPassword);
            await _employeeRepository.UpdateAsync(employee);

            TempData["Success"] = "Đổi mật khẩu thành công";
            return RedirectToAction("Index");
        }
    }
} 