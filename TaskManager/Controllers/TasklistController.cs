using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TaskManager.Models;
using System.Security.Claims;
using TaskManager.Models.Services;
using TaskManager.Helpers;
using TaskManager.ViewModels;

namespace TaskManager.Controllers;

[Authorize]
public class TasklistController : Controller {

    private readonly TasklistMethods _tasklistMethods;
    private readonly TaskMethods _taskMethods;
    private readonly ListUserMethods _listUserMethods;

    public TasklistController(TasklistMethods tasklistMethods, TaskMethods taskMethods, ListUserMethods listUserMethods) {
        _tasklistMethods = tasklistMethods;
        _taskMethods = taskMethods;
        _listUserMethods = listUserMethods;
    }

    public IActionResult Tasklists(string sortOrder) {

        List<TasklistModel> tasklists = new List<TasklistModel>();

        // Get the logged-in user's Id
        int? userId = User.GetUserId();

        if (userId.HasValue) {
            // Fetch task lists
            tasklists = _tasklistMethods.GetTasklistsForUser(userId.Value);
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
        int? userId = User.GetUserId();

        if (userId.HasValue) {
            string errorMsg;

            if (_tasklistMethods.CreateTasklist(userId.Value, newList, out errorMsg)) {
                return RedirectToAction("Tasklists");
            } else {
                // Display the error message in the view
                ModelState.AddModelError(string.Empty, errorMsg);
                
                return View(newList); // Show the form again with the error messages
            }
        } else {
            // Handle case where user is not properly logged in or identity is misconfigured
            return RedirectToAction("Login", "User");
        }
    }


    public IActionResult Tasklist(int listId) {
        // Get the user ID
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        // Retrieve tasklist details
        var tasklistDetails = _tasklistMethods.GetTasklistById(listId, userId.Value);

        // Retrieve tasks for the tasklist
        var tasks = _taskMethods.GetTasksByListId(listId);

        // Retrieve contributors for the tasklist
        var contributors = _listUserMethods.GetContributorsByListId(listId);

        // Construct the ViewModel
        var tasklistDetail = new TasklistDetailViewModel {
            TasklistId = tasklistDetails.Id, // Assuming tasklistDetails has Id
            Title = tasklistDetails.Title,
            Description = tasklistDetails.Description,
            CreatedAt = tasklistDetails.CreatedAt,
            CreatedByUsername = tasklistDetails.CreatedByUserName,
            UserRole = tasklistDetails.UserRole,
            Tasks = tasks,
            Contributors = contributors
        };

        // Pass the data to the view
        return View(tasklistDetail);
    }

}