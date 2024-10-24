using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TaskManager.Models;
using System.Security.Claims;
using TaskManager.Models.Services;

namespace TaskManager.Controllers;

[Authorize]
public class TasklistController : Controller {

    public IActionResult Tasklists(string sortOrder) {

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
    // Create tasklist form
    return View();
    }


    [HttpPost]
    public IActionResult Create(TasklistModel newList) {

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