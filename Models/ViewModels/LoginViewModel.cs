using System.ComponentModel.DataAnnotations;

namespace BilliardsManagement.Models.ViewModels;
public class LoginViewModel
{
    [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; }

    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; }

    // [Display(Name = "Ghi nhớ đăng nhập")]
    // public bool RememberMe { get; set; }
}
