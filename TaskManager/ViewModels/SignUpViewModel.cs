using System;
using System.ComponentModel.DataAnnotations;
namespace TaskManager.ViewModels;

public class SignUpViewModel
{

    [Required(ErrorMessage = "Username is required")]
    [StringLength(30, ErrorMessage = "Username cannot be longer than 30 characters.")]
    public string? UserName { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Must be a valid email address")]
    [StringLength(100, ErrorMessage = "Email cannot be longer than 100 characters.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be 8-100 characters long")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string? ConfirmPassword { get; set; }

}