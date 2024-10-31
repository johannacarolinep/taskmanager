using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TaskManager.Models;
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


    public IActionResult Tasklist(int listId, string sortOrder, string searchString, List<int> selectedPriority, List<string> selectedStatus) {
        // Get the user ID
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        // Retrieve tasklist details
        var tasklistDetails = _tasklistMethods.GetTasklistById(listId, userId.Value);
        if (tasklistDetails == null) {
            return NotFound(); // Return 404 if tasklist not found
        }

        // Retrieve contributors for the tasklist
        var contributors = _listUserMethods.GetContributorsByListId(listId);

        // Retrieve tasks for the tasklist
        var tasks = _taskMethods.GetActiveTasksByListId(listId) ?? new List<TaskModel>();

        // Search functionality
        if (!string.IsNullOrEmpty(searchString)) {
                tasks = tasks.Where(t => t.Description.Contains(
                    searchString, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Filter tasks by selected priorities
        if (selectedPriority != null && selectedPriority.Any()) {
            tasks = tasks.Where(t => selectedPriority.Contains(t.Priority)).ToList();
        }

        // Filter tasks by selected statuses
        if (selectedStatus != null && selectedStatus.Any()) {
            tasks = tasks.Where(t => selectedStatus.Contains(t.Status.ToString())).ToList();
        }

        // Sort tasks based on the sortOrder parameter
        switch (sortOrder) {
            case "description":
                tasks = tasks.OrderBy(t => t.Description).ToList();
                break;
            case "deadline":
                tasks = tasks.OrderBy(t => t.Deadline).ToList();
                break;
            default:
                // Default sort (optional)
                tasks = tasks.OrderBy(t => t.Deadline).ToList();
                break;
        }

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
        ViewBag.SearchString = searchString;
        ViewBag.SortOrder = sortOrder;
        ViewBag.SelectedPriority = selectedPriority;
        ViewBag.SelectedStatus = selectedStatus;
        return View(tasklistDetail);
    }


    [HttpGet]
    public IActionResult Delete(int listId) {
        // Make sure user has permission to delete tasklist
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        var userRole = _listUserMethods.GetUserRoleInList(userId.Value, listId);
        if (userRole != UserListRole.Owner) {
            return Forbid();
        }

        // Retrieve the tasklist to show in the confirmation view
        var tasklist = _tasklistMethods.GetTasklistById(listId, userId.Value); // Ensure this method retrieves the full tasklist model
        if (tasklist == null) {
            return NotFound(); // Handle case where tasklist does not exist
        }

        return View(tasklist); // Pass the tasklist model to the view
    }


    // Action to delete the tasklist
    [HttpPost]
    public IActionResult Delete(TasklistModel tasklist) {
        // Ensure the user has permission to delete the tasklist
        int? userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "User");
        }

        var userRole = _listUserMethods.GetUserRoleInList(userId.Value, tasklist.Id);
        if (userRole != UserListRole.Owner){
            return Forbid(); // Only Owner can delete
        }

        // Set the tasklist and its tasks as inactive. delete the listuser
        string errorMsg = "";
        _tasklistMethods.SoftDeleteTasklist(tasklist.Id, userId.Value, out errorMsg);

        if (string.IsNullOrEmpty(errorMsg)) {
            TempData["SuccessMessage"] = "Tasklist deleted successfully.";
            return RedirectToAction("Tasklists");
        }

        ModelState.AddModelError(string.Empty, errorMsg);
        return View(tasklist);
    }


    public IActionResult EditList(int listId) {
        // Ensure the user has permission to edit the tasklist
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        var userRole = _listUserMethods.GetUserRoleInList(userId.Value, listId);
        if (userRole != UserListRole.Owner && userRole != UserListRole.Admin) {
            return Forbid(); // Only Owner or Admin can edit
        }

        // Fetch the tasklist details for the view
        TasklistModel tasklist = _tasklistMethods.GetTasklistById(listId, userId.Value);
        if (tasklist == null) {
            return NotFound();
        }

        if (tasklist.IsActive == false) {
            return NotFound();
        }

        return View(tasklist);
    }


    [HttpPost]
    public IActionResult EditList(TasklistModel model) {
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        // Confirm the user still has permission
        var userRole = _listUserMethods.GetUserRoleInList(userId.Value, model.Id);
        if (userRole != UserListRole.Owner && userRole != UserListRole.Admin) {
            return Forbid();
        }

        if (!ModelState.IsValid) {
            return View(model);
        }

        string errorMsg = "";
        bool success = _tasklistMethods.UpdateTasklist(model, out errorMsg);

        if (success) {
            TempData["SuccessMessage"] = "Tasklist updated successfully.";
            return RedirectToAction("Tasklists");
        }

        ModelState.AddModelError(string.Empty, errorMsg);
        return View(model);
    }
}

