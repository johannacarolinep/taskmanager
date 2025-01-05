using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using TaskManager.Models;
using TaskManager.ViewModels;
using TaskManager.Models.Services;

namespace TaskManager.Controllers
{
    public class UserController : Controller
    {
        private readonly UserManager<UserModel> _userManager;
        private readonly SignInManager<UserModel> _signInManager;
        private readonly Cloudinary _cloudinary;

        private readonly ListUserMethods _listUserMethods;
        private readonly TasklistMethods _tasklistMethods;
        private readonly EncryptionHelper _encryptionHelper;
        private readonly UserMethods _userMethods;

        private readonly EmailService _emailService;

        public UserController(UserManager<UserModel> userManager, SignInManager<UserModel> signInManager, Cloudinary cloudinary, ListUserMethods listUserMethods, TasklistMethods tasklistMethods, IConfiguration configuration, UserMethods userMethods, EmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _cloudinary = cloudinary;
            _listUserMethods = listUserMethods;
            _tasklistMethods = tasklistMethods;
            _encryptionHelper = new EncryptionHelper(configuration);
            _userMethods = userMethods;
            _emailService = emailService;
        }

        private bool IsLoggedIn()
        {
            return User.Identity?.IsAuthenticated ?? false;
        }

        [HttpGet]
        public IActionResult Signup()
        {
            if (IsLoggedIn())
            {
                return RedirectToAction("Tasklists", "Tasklist");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Signup(SignUpViewModel tempUser)
        {
            
            if (!ModelState.IsValid) {
                return View(tempUser);
            }

            // Check if the email exists in either the active or deleted users
            var existingUser = await _userManager.FindByEmailAsync(tempUser.Email);
            if (existingUser != null) {
                ModelState.AddModelError(string.Empty, "An active account with this email already exists.");
                return View(tempUser);
            }

            string encryptedEmail = _encryptionHelper.Encrypt(tempUser.Email);
            var deletedUser = await _userMethods.FindDeletedByEmailAsync(encryptedEmail);

            if (deletedUser != null) {
                TempData["ReactivatePrompt"] = $"An account with email {tempUser.Email} was previously deactivated. Would you like to reactivate your account?";
                return RedirectToAction("ReactivateAccount");
            }

            string encryptedUsername = _encryptionHelper.Encrypt(tempUser.UserName);

            // Check for username uniqueness
            if (_userMethods.CheckIfUsernameExists(tempUser.UserName, encryptedUsername)) {
                ModelState.AddModelError("UserName", "This username is already taken.");
                return View(tempUser);
            }

            // Create a user object to store
            var user = new UserModel
            {
                UserName = tempUser.UserName,
                Email = tempUser.Email,
            };

            // Attempt to create the user
            var result = await _userManager.CreateAsync(user, tempUser.Password);

            if (result.Succeeded) {
                // Attempt to update pending invitations to use the new user Id
                var createdUser = await _userManager.FindByEmailAsync(tempUser.Email);
        
                // Call ListUserMethods to update invitations with this email
                string errorMsg;
                _listUserMethods.AssignUserIdToInvitations(createdUser.Id, createdUser.Email, out errorMsg);

                if (!string.IsNullOrEmpty(errorMsg)) {
                    TempData["ErrorMessage"] = errorMsg;
                }
                return RedirectToAction("Login", "User");
            }

            // Add errors to the ModelState
            foreach (var error in result.Errors) {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(tempUser);
        }


        [HttpGet]
        public IActionResult Login() {
            if (IsLoggedIn()) {
                return RedirectToAction("Tasklists", "Tasklist");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded) {
                // Fetch the current user from the UserManager
                var user = await _userManager.FindByNameAsync(model.UserName);

                if (user != null) {
                    // Update the LastLogin field with the current time
                    user.LastLogin = DateTime.Now;

                    // Update the user record in the database
                    await _userManager.UpdateAsync(user);
                }

                return RedirectToAction("Tasklists", "Tasklist");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout() {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }


        [HttpGet]
        public IActionResult ReactivateAccount() {
            if (IsLoggedIn()) {
                return RedirectToAction("Tasklists", "Tasklist");
            }

            return View();
        }


[HttpPost]
public async Task<IActionResult> ReactivateAccount(ReactivateAccountViewModel model) {
    if (!ModelState.IsValid) {
        return View(model);
    }

    string encrypted = _encryptionHelper.Encrypt(model.EmailOrUsername.ToLower());
    var deletedUser = await _userMethods.FindDeletedByEmailOrUserNameAsync(encrypted);

    if (deletedUser == null) {
        ModelState.AddModelError(string.Empty, "Sorry, either the account does not exist, or the credentials were invalid.");
        return View(model);
    }

    var existingUser = await _userManager.FindByIdAsync(deletedUser.UserId.ToString());

    if (existingUser == null) {
        ModelState.AddModelError(string.Empty, "Sorry, either the account does not exist, or the credentials were invalid.");
        return View(model);
    }

    // found both deletedUser and user objects

    // Verify the password (you need to implement this in your UserManager)
    var passwordCheck = await _userManager.CheckPasswordAsync(existingUser, model.Password);
    if (!passwordCheck) {

        ModelState.AddModelError(string.Empty, "Sorry, either the account does not exist, or the credentials were invalid.");
        return View(model);
    }

    // Proceed to reactivate the account
    existingUser.UserName = _encryptionHelper.Decrypt(deletedUser.UserNameEncrypted);
    existingUser.Email =_encryptionHelper.Decrypt(deletedUser.EmailEncrypted);
    existingUser.IsActive = true;

    var updateResult = await _userManager.UpdateAsync(existingUser);

    if (!updateResult.Succeeded) {
        ModelState.AddModelError(string.Empty, "Sorry, your credentials were valid but something went wrong. Please contact support.");
        return View(model);
    }

    _userMethods.DeleteDeletedUser(deletedUser.Id);


    TempData["SuccessMessage"] = "Your account has been successfully reactivated.";
    return RedirectToAction("Login");
}


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AccountCenter() {

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound("User not found."); // Handle the case where user is null
            }

            var model = new AccountCenterViewModel
            {
                CurrentUserName = user.UserName,
                CurrentEmail = user.Email,
                CurrentImage = user.Image
            };

            return View(model);
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateUsername(string newUserName) {

            // Get the current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var originalUserName = user.UserName;
            // Update the username
            user.UserName = newUserName;

            // Validate the username
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Sign in user with new credentials
                await _signInManager.SignInAsync(user, isPersistent: false);

                TempData["SuccessMessage"] = "Username updated successfully.";
                return RedirectToAction("AccountCenter");
            }

            // If the update failed, add errors to the ModelState
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("newUserName", error.Description);
            }

            // Return to the Account Center view with the model to show errors
            return View("AccountCenter", new AccountCenterViewModel
            {
                CurrentUserName = originalUserName,
                CurrentEmail = user.Email,
                CurrentImage = user.Image
            });
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateEmail(string newEmail) {

            // Get the current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound("User not found.");
            }

            var originalEmail = user.Email; // Store the original email in case of failure

            // Update the email
            user.Email = _userManager.NormalizeEmail(newEmail);

            // Validate the email and attempt to update
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Optionally, sign in the user again if required
                await _signInManager.SignInAsync(user, isPersistent: false);

                // Add success message
                TempData["SuccessMessage"] = "Email updated successfully.";
                return RedirectToAction("AccountCenter");
            }

            // If the update failed, add errors to the ModelState
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("newEmail", error.Description);
            }

            // Return to the Account Center view with the original email in case of failure
            return View("AccountCenter", new AccountCenterViewModel
            {
                CurrentUserName = user.UserName,
                CurrentEmail = originalEmail,
                CurrentImage = user.Image
            });
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdatePassword(AccountCenterViewModel model) {

            // Validate the model
            if (!ModelState.IsValid) {
                // Get the current user to pass back to the view, even if there's an error
                var userForError = await _userManager.GetUserAsync(User);
                if (userForError == null)
                {
                    return NotFound("User not found.");
                }

                model.CurrentUserName = userForError.UserName;
                model.CurrentEmail = userForError.Email;
                return View("AccountCenter", model);
            }

            // Get the current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Verify the current password
            var passwordCheck = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
            if (!passwordCheck) {
                ModelState.AddModelError("CurrentPassword", "The current password is incorrect.");
                model.CurrentUserName = user.UserName;
                model.CurrentEmail = user.Email;

                return View("AccountCenter", model);
            }

            // Attempt to change the password
            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                // Optionally, sign the user in again
                await _signInManager.SignInAsync(user, isPersistent: false);

                // Add a success message
                TempData["SuccessMessage"] = "Password updated successfully.";
                return RedirectToAction("AccountCenter");
            }

            // If the update failed, add errors to the ModelState
            foreach (var error in result.Errors) {
                ModelState.AddModelError("NewPassword", error.Description);
            }

            model.CurrentUserName = user.UserName;
            model.CurrentEmail = user.Email;
            model.CurrentImage = user.Image;            

            return View("AccountCenter", model);
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateProfileImage(AccountCenterViewModel model) {

            // Ensure an image is selected
            if (model.ProfileImage == null || model.ProfileImage.Length == 0) {
                ModelState.AddModelError("ProfileImage", "Please select an image to upload.");
                return View("AccountCenter", model); // Return the same view with errors - need to add back in the username etc
            }

            // Get the current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound("User not found.");
            }

            // Temporarily store the old image url
            var oldImageUrl = user.Image;

            // Upload new image to cloudinary and update user
            try {
                // Upload the image to Cloudinary
                var uploadResult = await UploadImageToCloudinary(model.ProfileImage);

                // If the upload was successful, update the user's image URL
                if (uploadResult != null && !string.IsNullOrEmpty(uploadResult.SecureUrl.ToString())) {

                    user.Image = uploadResult.SecureUrl.ToString();
                    var updateResult = await _userManager.UpdateAsync(user);

                    // If the update was successful, delete the old image
                    if (updateResult.Succeeded) {
                        // delete old image from cloudinary, except default image
                        var defaultImageUrl = "https://res.cloudinary.com/deceun0wd/image/upload/v1716381152/default_profile_shke8m.jpg";

                        if (!string.IsNullOrEmpty(oldImageUrl) && oldImageUrl != defaultImageUrl) {
                            var oldImagePublicId = GetCloudinaryPublicId(oldImageUrl);
                            await DeleteImageFromCloudinary(oldImagePublicId);
                        }

                        TempData["SuccessMessage"] = "Profile image updated successfully.";
                        return RedirectToAction("AccountCenter");
                    }

                    // Add any errors that occurred while updating the user
                    foreach (var error in updateResult.Errors) {
                        ModelState.AddModelError("ProfileImage", error.Description);
                    }
                } else {
                    ModelState.AddModelError("ProfileImage", "Failed to upload image.");
                }
            } catch (Exception ex) {
                ModelState.AddModelError("ProfileImage", $"An error occurred: {ex.Message}");
            }

            // Return to the Account Center view with the model and errors
            return View("AccountCenter", new AccountCenterViewModel {
                CurrentUserName = user.UserName,
                CurrentEmail = user.Email,
                CurrentImage = oldImageUrl
            });
        }


        private async Task<ImageUploadResult> UploadImageToCloudinary(IFormFile imageFile) {
            // Convert IFormFile to a stream to upload to Cloudinary
            var uploadResult = new ImageUploadResult();

            if (imageFile.Length > 0) {
                await using var stream = imageFile.OpenReadStream();

                var uploadParams = new ImageUploadParams() {
                    File = new FileDescription(imageFile.FileName, stream),
                    Transformation = new Transformation().Width(200).Height(200).Crop("fill")
                };

                // Upload the image to Cloudinary
                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }
            return uploadResult;
        }

        private string GetCloudinaryPublicId(string imageUrl) {
            // Extract the cloudinary public ID from the imageURL
            var idString = imageUrl.Split('/').Last();
            return idString.Substring(0, idString.LastIndexOf('.')); // Return without file extension
        }

        private async Task<DeletionResult> DeleteImageFromCloudinary(string publicId) {
            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);
            return result;
        }


        [Authorize]
        [HttpGet]
        public IActionResult DeactivateAccount() {
            
            // Display the form to confirm deactivation
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeactivateAccount(DeactivateAccountViewModel model) {
            // Get the current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound("User not found.");
            }

            // Validate the password and confirmation
            if (!ModelState.IsValid) {
                return View(model);
            }

            // Verify the current password
            var passwordCheck = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!passwordCheck) {
                ModelState.AddModelError("Password", "Incorrect password. Please try again.");
                return View(model);
            }

            // Proceed with account deactivation
            var result = await _userManager.DeleteAsync(user);

            // Pass back error messages if not successful
            if (!result.Succeeded) {
                foreach (var error in result.Errors) {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            // Attempt to delete user's related entities.
            string errorMsg = "";
            string currentUserName = User.Identity.Name;
            List<TasklistModel> tasklists = _tasklistMethods.GetTasklistsForUser(user.Id, currentUserName);
            foreach(var tasklist in tasklists) {
                if (tasklist.UserRole == "Owner") {
                    _tasklistMethods.SoftDeleteTasklist(tasklist.Id, user.Id, out errorMsg);
                } else {
                    _listUserMethods.DeleteListUserByUserAndList(user.Id, tasklist.Id, out errorMsg);
                }
            }

            string successMessage = "Your account has been successfully deactivated.";
            if (!string.IsNullOrEmpty(errorMsg)) {
                successMessage += " However, some of your related entities may have failed to delete. If this is a problem, please contact email@email.com.";
            }
            // Sign out the user
            TempData["SuccessMessage"] = successMessage;
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }


        [HttpGet]
        public IActionResult ForgotPassword() {

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model) {
            if (!ModelState.IsValid) {
                return View(model);
            }

            // Find the user by email
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) {
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            // Generate password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action("ResetPassword", "User", new { token, email = model.Email }, Request.Scheme);

            // Prepare email content
            string subject = "Password reset for your TaskManager account";
            string plainString = $"Please reset your password by clicking here: <a href='{resetLink}'>link</a>";
            string HtmlContent = $@"
                <h2>Your TaskManager account password was reset</h2>
                <p>Hello <strong>{user.UserName}</strong>!</p>
                <p>Your password was successfully reset. Click the link below to update your password and access the account:</p>
                <p><a href='{resetLink}' style='color: #1E90FF; text-decoration: none;'>Update your password</a></p>
                <br />
                <p>Best regards,<br />The TaskManager Team</p>";

            // Send email with the reset link
            await _emailService.SendEmailAsync(model.Email, subject, plainString, HtmlContent);

            return RedirectToAction("ForgotPasswordConfirmation");
        }


        [HttpGet]
        public IActionResult ResetPassword(string token, string email) {
            var model = new ResetPasswordViewModel { Token = token, Email = email };
            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model) {
            if (!ModelState.IsValid) {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            // Reset the password
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded) {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors) {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation() {
            return View();
        }


        [HttpGet]
        public IActionResult ResetPasswordConfirmation() {
            return View();
        }


    }
}