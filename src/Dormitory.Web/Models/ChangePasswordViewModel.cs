using System.ComponentModel.DataAnnotations;
 
namespace Dormitory.Web.Models;
 
public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Введіть поточний пароль")]
    [DataType(DataType.Password)]
    [Display(Name = "Поточний пароль")]
    public string OldPassword { get; set; } = null!;
 
    [Required(ErrorMessage = "Введіть новий пароль")]
    [DataType(DataType.Password)]
    [Display(Name = "Новий пароль")]
    public string NewPassword { get; set; } = null!;
 
    [Required(ErrorMessage = "Підтвердіть новий пароль")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Паролі не співпадають")]
    [Display(Name = "Підтвердження нового пароля")]
    public string ConfirmPassword { get; set; } = null!;
}