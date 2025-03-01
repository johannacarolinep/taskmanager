using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TaskManager.Models;


public class HomeController : Controller {
    private readonly SignInManager<UserModel> _signInManager;

    public HomeController(SignInManager<UserModel> signInManager) {
        _signInManager = signInManager;
    }

    public IActionResult Index() {
        if (_signInManager.IsSignedIn(User)) {
            return RedirectToAction("Tasklists", "Tasklist");
        }
        return View();
    }
}
