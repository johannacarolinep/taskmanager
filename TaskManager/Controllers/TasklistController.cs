using Microsoft.AspNetCore.Mvc;
using TaskManager.Models;
using System.Security.Claims;
using TaskManager.Models.Services;

namespace TaskManager.Controllers;

public class TasklistController : Controller {

    private bool IsLoggedIn() {
        return User.Identity?.IsAuthenticated ?? false;
    }

    public IActionResult Tasklists(string sortOrder) {
        // Redirect non-logged-in users
        if (!IsLoggedIn()) {
            return RedirectToAction("Login", "User");
        }

        List<TasklistModel> tasklists = new List<TasklistModel>();

        // Get the logged-in user's Id
        string userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out int userId)) {
            // Fetch task lists
            TasklistMethods tm = new TasklistMethods();
            tasklists = tm.GetTasklistsForUser(userId);
        } else {
            // Handle case where user is not properly logged in or identity is misconfigured
            return RedirectToAction("Login", "User");
        }

        // Sort task lists based on the sortOrder parameter
        switch (sortOrder) {
            case "title":
                tasklists = tasklists.OrderBy(t => t.Title).ToList();
                break;
            case "date":
                tasklists = tasklists.OrderBy(t => t.CreatedAt).ToList();
                break;
            default:
                // Default sort by creation date
                tasklists = tasklists.OrderBy(t => t.CreatedAt).ToList();
                break;
        }

        ViewBag.SortOrder = sortOrder;

        // Return the task lists to the view
        return View(tasklists);
    }


    public IActionResult Create() {
    // redirects non logged in users to the login view
    if (!IsLoggedIn()) {
        return RedirectToAction("Login", "User");
    }
    // Create tasklist form
    return View();
    }


    [HttpPost]
    public IActionResult Create(TasklistModel newList) {
        // redirects non logged in users to the login view
        if (!IsLoggedIn()) {
            return RedirectToAction("Login", "User");
        }

        if (!ModelState.IsValid) {
            return View(newList);
        }

        // Get the logged-in user's Id
        string userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out int userId)) {
            // logic here?
            TasklistMethods tm = new TasklistMethods();
            string errorMsg;

            // Call the method and handle the success or failure
            if (tm.CreateTasklist(userId, newList, out errorMsg)) {
                return RedirectToAction("Tasklists");
            } else {
                // Display the error message in the view
                ViewBag.error = errorMsg;
                
                return View(newList); // Show the form again with the error message
            }
        } else {
            // Handle case where user is not properly logged in or identity is misconfigured
            return RedirectToAction("Login", "User");
        }
    }

}