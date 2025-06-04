using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;
using BilliardsManagement.Models.ViewModels;

namespace BilliardsManagement.Controllers
{
    public class ProfileController : Controller
    {
        private readonly BilliardsDbContext _context;

        public ProfileController(BilliardsDbContext context)
        {
            _context = context;
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

            var employee = await _context.Employees.FindAsync(employeeId.Value);
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

            if (employeeId.Value != model.EmployeeId)
            {
                TempData["Error"] = "Không có quyền chỉnh sửa thông tin này";
                return RedirectToAction(nameof(Index));
            }

            var employee = await _context.Employees.FindAsync(employeeId.Value);
            if (employee == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                TempData["Error"] = "Họ tên không được để trống";
                return RedirectToAction(nameof(Index));
            }

            // Check if email is already used by another employee
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var emailExists = await _context.Employees
                    .AnyAsync(e => e.Email == model.Email && e.EmployeeId != employeeId.Value);
                if (emailExists)
                {
                    TempData["Error"] = "Email này đã được sử dụng bởi tài khoản khác";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Update employee information
            employee.FullName = model.FullName.Trim();
            employee.Phone = model.Phone?.Trim();
            employee.Email = model.Email?.Trim();

            await _context.SaveChangesAsync();

            // Update session if full name changed
            HttpContext.Session.SetString("FullName", employee.FullName);

            TempData["Success"] = "Cập nhật thông tin thành công";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var employeeId = HttpContext.Session.GetInt32("EmployeeId");
            if (!employeeId.HasValue)
            {
                return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn" });
            }

            var employee = await _context.Employees.FindAsync(employeeId.Value);
            if (employee == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin tài khoản" });
            }

            // Validate input
            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                return Json(new { success = false, message = "Vui lòng nhập mật khẩu hiện tại" });
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                return Json(new { success = false, message = "Mật khẩu mới phải có ít nhất 6 ký tự" });
            }

            if (newPassword != confirmPassword)
            {
                return Json(new { success = false, message = "Mật khẩu xác nhận không khớp" });
            }

            // Verify current password
            var hashedCurrentPassword = BilliardsManagement.Helpers.PasswordHasher.ComputeSha256Hash(currentPassword);
            if (employee.Password != hashedCurrentPassword)
            {
                return Json(new { success = false, message = "Mật khẩu hiện tại không đúng" });
            }

            // Update password
            employee.Password = BilliardsManagement.Helpers.PasswordHasher.ComputeSha256Hash(newPassword);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đổi mật khẩu thành công" });
        }
    }
} 