using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManager.Models;
using TaskManager.ViewModels;

namespace TaskManager.Controllers
{
    public class UserController : Controller
    {
        private readonly UserManager<UserModel> _userManager;
        private readonly SignInManager<UserModel> _signInManager;

        public UserController(UserManager<UserModel> userManager, SignInManager<UserModel> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
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

            if (result.Succeeded)
            {
                return RedirectToAction("Login", "User");
            }

            // Add errors to the ModelState
            foreach (var error in result.Errors)
            {
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


        [HttpGet]
        public async Task<IActionResult> AccountCenter()
        {
            if (!IsLoggedIn()) {
                RedirectToAction("Login");
            }

            // Log claims for debugging
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"User ID from claims: {userId}");
            Console.WriteLine($"User: {User}");

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound("User not found."); // Handle the case where user is null
            }
            Console.WriteLine($"user: {user}");

            var model = new AccountCenterViewModel
            {
                CurrentUserName = user.UserName,
                CurrentEmail = user.Email,
                CurrentImage = user.Image
            };

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> UpdateUsername(string newUserName) {
            // Ensure the user is logged in
            if (!IsLoggedIn()) {
                return RedirectToAction("Login");
            }

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


        [HttpPost]
        public async Task<IActionResult> UpdateEmail(string newEmail) {
            // Ensure the user is logged in
            if (!IsLoggedIn()) {
                return RedirectToAction("Login");
            }

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


        [HttpPost]
        public async Task<IActionResult> UpdatePassword(AccountCenterViewModel model)
        {
            // Ensure the user is logged in
            if (!IsLoggedIn()) {
                return RedirectToAction("Login");
            }

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

            Console.WriteLine("Passed password check");

            // Attempt to change the password
            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            Console.WriteLine($"Results: {result}");

            if (result.Succeeded)
            {
                Console.WriteLine("Password updated");
                // Optionally, sign the user in again
                await _signInManager.SignInAsync(user, isPersistent: false);

                Console.WriteLine("After re-login");
                // Add a success message
                TempData["SuccessMessage"] = "Password updated successfully.";
                return RedirectToAction("AccountCenter");
            }

            // If the update failed, add errors to the ModelState
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("NewPassword", error.Description);
            }

            model.CurrentUserName = user.UserName;
            model.CurrentEmail = user.Email;            

            return View("AccountCenter", model);
        }

    }
}