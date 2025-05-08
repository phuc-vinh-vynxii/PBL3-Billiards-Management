using System.ComponentModel.DataAnnotations;

namespace BilliardsManagement.Models.ViewModels;

public class RegisterViewModel
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

    [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống.")]
    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu")]
    [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp.")]
    public string ConfirmPassword { get; set; }

    [Required(ErrorMessage = "Họ và tên không được để trống.")]
    [Display(Name = "Họ và tên")]
    [RegularExpression(@"^[A-ZÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ][a-zàáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ]*(?:[ ][A-ZÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ][a-zàáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ]*)*$",
        ErrorMessage = "Họ và tên phải bắt đầu bằng chữ in hoa, có thể có dấu, độ dài từ 2-50 ký tự.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Họ và tên phải có độ dài từ 2-50 ký tự.")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [Display(Name = "Email")]
    [RegularExpression(@"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$",
        ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Số điện thoại không được để trống.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [Display(Name = "Số điện thoại")]
    [RegularExpression(@"^(0|\+84)[3|5|7|8|9][0-9]{8}$",
        ErrorMessage = "Số điện thoại không đúng định dạng (VD: 0912345678 hoặc +84912345678).")]
    public string Phone { get; set; }
}