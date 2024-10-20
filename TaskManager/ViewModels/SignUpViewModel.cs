using System;
using System.ComponentModel.DataAnnotations;
namespace TaskManager.ViewModels;

public class SignUpViewModel
{

    [Required(ErrorMessage = "Username is required")]
    [StringLength(30, MinimumLength = 1, ErrorMessage = "Username must be between 1 and 30 characters.")]
    public string? UserName { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [StringLength(100, ErrorMessage = "Email must be below 100 characters.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string? ConfirmPassword { get; set; }

}