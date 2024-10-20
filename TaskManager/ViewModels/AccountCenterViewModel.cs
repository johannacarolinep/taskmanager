using System;
using System.ComponentModel.DataAnnotations;
namespace TaskManager.ViewModels;

public class AccountCenterViewModel
{
    // For Username Update
    public string? CurrentUserName { get; set; }

    [StringLength(30, MinimumLength = 1, ErrorMessage = "Username must be between 1 and 30 characters.")]
    public string? NewUserName { get; set; }

    // For Email Update
    public string? CurrentEmail { get; set; }

    [StringLength(100, ErrorMessage = "Email must be below 100 characters.")]
    public string? NewEmail { get; set; }

    // For Password Update
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }

    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    public string? ConfirmNewPassword { get; set; }

    // For Profile Image Upload
    public IFormFile? ProfileImage { get; set; }

    public string CurrentImage {get; set;}
}