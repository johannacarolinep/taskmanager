using System;
using Microsoft.AspNetCore.Identity;

namespace TaskManager.Models;

public class UserModel : IdentityUser<int> {
    // inherits fields from IdentityUser
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string Image { get; set; } = "https://res.cloudinary.com/deceun0wd/image/upload/v1716381152/default_profile_shke8m.jpg";

    public bool IsActive { get; set; } = true;

    public DateTime? LastLogin { get; set; }
}