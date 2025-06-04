using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;
using BilliardsManagement.Models.ViewModels;

namespace BilliardsManagement.Services
{
    public class StaffService : IStaffService
    {
        private readonly BilliardsDbContext _context;

        public StaffService(BilliardsDbContext context)
        {
            _context = context;
        }

        public async Task<StaffManagementViewModel> GetStaffManagementDataAsync()
        {
            var employees = await _context.Employees
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var statistics = new StaffStatistics
            {
                TotalEmployees = employees.Count,
                ManagerCount = employees.Count(e => e.Position == "MANAGER"),
                CashierCount = employees.Count(e => e.Position == "CASHIER"),
                ServingCount = employees.Count(e => e.Position == "SERVING"),
                NewEmployeesThisMonth = employees.Count(e => e.CreatedAt.HasValue && 
                    e.CreatedAt.Value.Month == currentMonth && 
                    e.CreatedAt.Value.Year == currentYear)
            };

            return new StaffManagementViewModel
            {
                Employees = employees,
                Statistics = statistics
            };
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            return await _context.Employees.FindAsync(id);
        }

        public async Task<(bool Success, string Message)> CreateEmployeeAsync(CreateEmployeeViewModel model)
        {
            // Validation
            if (string.IsNullOrEmpty(model.FullName) || string.IsNullOrEmpty(model.Username) || 
                string.IsNullOrEmpty(model.Password) || string.IsNullOrEmpty(model.Position))
            {
                return (false, "Vui lòng điền đầy đủ thông tin bắt buộc");
            }

            // Prevent creating new MANAGER employees
            if (model.Position == "MANAGER")
            {
                return (false, "Không thể tạo tài khoản quản lý mới");
            }

            if (model.Password != model.ConfirmPassword)
            {
                return (false, "Mật khẩu xác nhận không khớp");
            }

            // Check if username already exists
            if (await _context.Employees.AnyAsync(e => e.Username == model.Username))
            {
                return (false, "Tên đăng nhập đã tồn tại");
            }

            // Check if email already exists (if provided)
            if (!string.IsNullOrEmpty(model.Email) && 
                await _context.Employees.AnyAsync(e => e.Email == model.Email))
            {
                return (false, "Email đã được sử dụng");
            }

            var employee = new Employee
            {
                FullName = model.FullName,
                Position = model.Position,
                Phone = model.Phone,
                Email = model.Email,
                Username = model.Username,
                Password = BilliardsManagement.Helpers.PasswordHasher.ComputeSha256Hash(model.Password),
                CreatedAt = DateTime.Now
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return (true, "Thêm nhân viên thành công");
        }

        public async Task<(bool Success, string Message)> UpdateEmployeeAsync(EditEmployeeViewModel model)
        {
            var employee = await _context.Employees.FindAsync(model.EmployeeId);
            if (employee == null)
            {
                return (false, "Không tìm thấy nhân viên");
            }

            // Check if trying to edit a MANAGER - not allowed
            if (employee.Position == "MANAGER")
            {
                return (false, "Không thể chỉnh sửa thông tin người quản lý");
            }

            if (string.IsNullOrEmpty(model.FullName) || string.IsNullOrEmpty(model.Username) || 
                string.IsNullOrEmpty(model.Position))
            {
                return (false, "Vui lòng điền đầy đủ thông tin bắt buộc");
            }

            // Prevent setting position to MANAGER
            if (model.Position == "MANAGER")
            {
                return (false, "Không thể đặt chức vụ thành quản lý");
            }

            // Check if username is taken by another employee
            if (await _context.Employees.AnyAsync(e => e.Username == model.Username && e.EmployeeId != model.EmployeeId))
            {
                return (false, "Tên đăng nhập đã tồn tại");
            }

            // Check if email is taken by another employee (if provided)
            if (!string.IsNullOrEmpty(model.Email) && 
                await _context.Employees.AnyAsync(e => e.Email == model.Email && e.EmployeeId != model.EmployeeId))
            {
                return (false, "Email đã được sử dụng");
            }

            employee.FullName = model.FullName;
            employee.Position = model.Position;
            employee.Phone = model.Phone;
            employee.Email = model.Email;
            employee.Username = model.Username;

            await _context.SaveChangesAsync();

            return (true, "Cập nhật thông tin nhân viên thành công");
        }

        public async Task<(bool Success, string Message)> DeleteEmployeeAsync(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return (false, "Không tìm thấy nhân viên");
            }

            // Check if trying to delete a MANAGER - not allowed
            if (employee.Position == "MANAGER")
            {
                return (false, "Không thể xóa người quản lý");
            }

            try
            {
                // Handle related records before deleting employee
                
                // 1. Update Invoices - set CashierId to null (keep invoice for record but remove employee reference)
                var relatedInvoices = await _context.Invoices.Where(i => i.CashierId == id).ToListAsync();
                foreach (var invoice in relatedInvoices)
                {
                    invoice.CashierId = null; // Keep invoice for audit trail but remove employee reference
                }

                // 2. Update Sessions - set EmployeeId to null (keep session for record but remove employee reference)
                var relatedSessions = await _context.Sessions.Where(s => s.EmployeeId == id).ToListAsync();
                foreach (var session in relatedSessions)
                {
                    session.EmployeeId = null; // Keep session for audit trail but remove employee reference
                }

                // 3. Update Orders - set EmployeeId to null (keep order for record but remove employee reference)
                var relatedOrders = await _context.Orders.Where(o => o.EmployeeId == id).ToListAsync();
                foreach (var order in relatedOrders)
                {
                    order.EmployeeId = null; // Keep order for audit trail but remove employee reference
                }

                // 4. Remove employee permissions
                var employeePermissions = await _context.EmployeePermissions.Where(ep => ep.EmployeeId == id).ToListAsync();
                _context.EmployeePermissions.RemoveRange(employeePermissions);

                // Save changes to update related records
                await _context.SaveChangesAsync();

                // 5. Finally delete the employee
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();

                return (true, $"Đã xóa nhân viên {employee.FullName} thành công. Các dữ liệu liên quan đã được cập nhật để giữ lại lịch sử giao dịch.");
            }
            catch (Exception ex)
            {
                return (false, $"Có lỗi xảy ra khi xóa nhân viên: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(int id, string newPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                return (false, "Mật khẩu phải có ít nhất 6 ký tự");
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return (false, "Không tìm thấy nhân viên");
            }

            // Check if trying to reset password for a MANAGER - not allowed
            if (employee.Position == "MANAGER")
            {
                return (false, "Không thể đặt lại mật khẩu cho người quản lý");
            }

            employee.Password = BilliardsManagement.Helpers.PasswordHasher.ComputeSha256Hash(newPassword);
            await _context.SaveChangesAsync();

            return (true, "Đặt lại mật khẩu thành công");
        }
    }
} 