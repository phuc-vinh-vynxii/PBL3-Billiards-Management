using System.ComponentModel.DataAnnotations;

namespace BilliardsManagement.Models.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; }

    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    public string Password { get; set; }

    [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống.")]
    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu")]
    [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp.")]
    public string ConfirmPassword { get; set; }

    [Required(ErrorMessage = "Họ và tên không được để trống.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Số điện thoại không được để trống.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [Display(Name = "Số điện thoại")]
    public string Phone { get; set; }
}