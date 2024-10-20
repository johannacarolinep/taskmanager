using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
    }
}