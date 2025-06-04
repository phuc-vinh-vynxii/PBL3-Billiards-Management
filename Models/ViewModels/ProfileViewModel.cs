using System.ComponentModel.DataAnnotations;

namespace BilliardsManagement.Models.ViewModels
{
    public class ProfileViewModel
    {
        public int EmployeeId { get; set; }
        
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;
        
        [Display(Name = "Chức vụ")]
        public string? Position { get; set; }
        
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }
        
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string? Email { get; set; }
        
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;
        
        [Display(Name = "Ngày tạo tài khoản")]
        public DateTime? CreatedAt { get; set; }
    }
} 