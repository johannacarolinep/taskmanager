using System;
using System.ComponentModel.DataAnnotations;
namespace TaskManager.ViewModels;

public class LoginViewModel
{
    [Required]
    [Display(Name = "Username")]
    public string UserName { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    public bool RememberMe { get; set; }
}