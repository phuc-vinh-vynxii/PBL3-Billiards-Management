using System.ComponentModel.DataAnnotations;

namespace BilliardsManagement.Models.ViewModels;
public class LoginViewModel
{
    [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
    [Display(Name = "Tên đăng nhập")]
    [RegularExpression(@"^[a-zA-Z0-9_]{4,20}$", 
        ErrorMessage = "Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới, độ dài từ 4-20 ký tự.")]
    public string Username { get; set; }

    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$",
        ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.")]
    public string Password { get; set; }

    // [Display(Name = "Ghi nhớ đăng nhập")]
    // public bool RememberMe { get; set; }
}
