using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using TaskManager.Models;
using TaskManager.ViewModels;

namespace TaskManager.Controllers
{
    public class UserController : Controller
    {
        private readonly UserManager<UserModel> _userManager;
        private readonly SignInManager<UserModel> _signInManager;
        private readonly Cloudinary _cloudinary;

        private readonly ListUserMethods _listUserMethods;

        public UserController(UserManager<UserModel> userManager, SignInManager<UserModel> signInManager, Cloudinary cloudinary, ListUserMethods listUserMethods)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _cloudinary = cloudinary;
            _listUserMethods = listUserMethods;
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
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Signup(SignUpViewModel tempUser)
        {
            
            if (!ModelState.IsValid)
            {
                return View(tempUser);
            }

            // Create a user object to store
            var user = new UserModel
            {
                UserName = tempUser.UserName,
                Email = tempUser.Email,
            };

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
        public IActionResult Login()
        {
            if (IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine("Modelstat invalid, login");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);

            Console.WriteLine($"Result: {result}");
            if (result.Succeeded)
            {
                // Fetch the current user from the UserManager
                var user = await _userManager.FindByNameAsync(model.UserName);

                if (user != null) {
                    // Update the LastLogin field with the current time
                    user.LastLogin = DateTime.Now;

                    // Update the user record in the database
                    await _userManager.UpdateAsync(user);
                }

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
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
                // Add a success message (consider using TempData for flash messages)
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
                CurrentEmail = user.Email
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
                CurrentEmail = originalEmail
            });
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdatePassword(AccountCenterViewModel model) {

            // Validate the model
            if (!ModelState.IsValid)
            {
                Console.WriteLine("Modelstate was invalid in update password");
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
            if (!passwordCheck)
            {
                Console.WriteLine("Password did not pass check!!!");
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

                    // Add any errors that occurred while updating the user record
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
            // Extract the public ID from the URL
            var fileName = imageUrl.Split('/').Last(); // Get file name with extension
            return fileName.Substring(0, fileName.LastIndexOf('.')); // Return without file extension
        }

        private async Task<DeletionResult> DeleteImageFromCloudinary(string publicId) {
            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);
            return result;
        }

    }
}