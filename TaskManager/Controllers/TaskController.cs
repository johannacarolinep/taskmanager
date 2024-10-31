using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TaskManager.Models;
using System.Security.Claims;
using TaskManager.Models.Services;
using TaskManager.Helpers;
using TaskManager.ViewModels;

namespace TaskManager.Controllers;

[Authorize]
public class TaskController : Controller {

    private readonly TasklistMethods _tasklistMethods;
    private readonly TaskMethods _taskMethods;
    private readonly ListUserMethods _listUserMethods;

    public TaskController(TasklistMethods tasklistMethods, TaskMethods taskMethods, ListUserMethods listUserMethods) {
        _tasklistMethods = tasklistMethods;
        _taskMethods = taskMethods;
        _listUserMethods = listUserMethods;
    }

    public IActionResult Create(int listId) {

        // get the user id
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        // Check if the user has appropriate role
        var userRole = _listUserMethods.GetUserRoleInList(userId.Value, listId);
        if (userRole != UserListRole.Owner && userRole != UserListRole.Admin) {
            return Forbid(); // Return 403 if user lacks the required permissions
        }

        // Create tasklist form
        ViewBag.listId = listId;
        return View();
    }


    [HttpPost]
    public IActionResult Create(TaskModel newTask) {
    
        if (!ModelState.IsValid) {
            return View(newTask);
        }

        // get the user id
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        // Check if the user has appropriate role
        var userRole = _listUserMethods.GetUserRoleInList(userId.Value, newTask.ListId);
        if (userRole != UserListRole.Owner && userRole != UserListRole.Admin) {
            return Forbid(); // Return 403 if user lacks the required permissions
        }

        string errorMsg;
        _taskMethods.AddTask(newTask, out errorMsg);
        if (string.IsNullOrEmpty(errorMsg)) {
            return RedirectToAction("Tasklist", "Tasklist", new { listId = newTask.ListId });
        }

        ModelState.AddModelError(string.Empty, errorMsg);
        return View(newTask);
    }


    [HttpGet]
    public IActionResult Edit(int taskId) {
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        var task = _taskMethods.GetActiveTaskById(taskId);
        
        if (task == null) {
            return NotFound(); // Return 404 if task not found
        }

        // Check if the user has appropriate role
        var userRole = _listUserMethods.GetUserRoleInList(userId.Value, task.ListId);
        if (userRole != UserListRole.Owner && userRole != UserListRole.Admin) {
            return Forbid(); // Return 403 if user lacks the required permissions
        }

        return View(task); // Pass task to the Edit view
    }


    [HttpPost]
    public IActionResult Edit(TaskModel updatedTask) {
        if (!ModelState.IsValid) {
            return View(updatedTask);
        }

        // get the user id
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        // Check if the user has appropriate role
        var userRole = _listUserMethods.GetUserRoleInList(userId.Value, updatedTask.ListId);
        if (userRole != UserListRole.Owner && userRole != UserListRole.Admin) {
            return Forbid(); // Return 403 if user lacks the required permissions
        }

        // Attempt to update the task
        string errorMsg;
        if (_taskMethods.UpdateTask(updatedTask, out errorMsg)) {
            return RedirectToAction("Tasklist", "Tasklist", new { listId = updatedTask.ListId });
        }

        // If update failed, add an error to the model state and redisplay the form
        ModelState.AddModelError(string.Empty, errorMsg);
        return View(updatedTask);
    }


    [HttpGet]
    public IActionResult Delete(int taskId) {
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        var task = _taskMethods.GetActiveTaskById(taskId);
        
        if (task == null) {
            return NotFound(); // Return 404 if task not found
        }

        // Check if the user has appropriate role
        var userRole = _listUserMethods.GetUserRoleInList(userId.Value, task.ListId);
        if (userRole != UserListRole.Owner && userRole != UserListRole.Admin) {
            return Forbid(); // Return 403 if user lacks the required permissions
        }

        return View(task); // Pass task to the Edit view
    }


    [HttpPost]
    public IActionResult Delete(TaskModel task) {
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        // Check if the user has permission to delete this task
        var userRole = _listUserMethods.GetUserRoleInList(userId.Value, task.ListId);
        if (userRole != UserListRole.Owner && userRole != UserListRole.Admin) {
            return Forbid(); // Return 403 if unauthorized
        }

        // Attempt to delete the task
        string errorMsg;
        if (_taskMethods.SoftDeleteTask(task.Id, out errorMsg)) {
            return RedirectToAction("Tasklist", "Tasklist", new { listId = task.ListId });
        }

        // If deletion failed, display error and reload view
        ModelState.AddModelError(string.Empty, errorMsg);
        return View(task); // Redisplay the delete view with error message
    }


    [HttpPost]
    public IActionResult Progress(int taskId) {
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        var task = _taskMethods.GetActiveTaskById(taskId);
        if (task == null || task.Status == TaskManager.Models.TaskStatus.Done || task.Status == TaskManager.Models.TaskStatus.InProgress) {
            return NotFound(); // Task doesn't exist or not in the required state
        }

        // Check if the user has access (Contributor, Admin, or Owner)
        var userRole = _listUserMethods.GetUserRoleInList(userId.Value, task.ListId);
        if (userRole == null) {
            return Forbid();
        }

        // Update task status to In Progress
        task.Status = TaskManager.Models.TaskStatus.InProgress;
        string errorMsg;
        if (!_taskMethods.UpdateTask(task, out errorMsg)){
            TempData["ErrorMessage"] = errorMsg;
            return RedirectToAction("Tasklist", "Tasklist", new { listId = task.ListId });
        }

        return RedirectToAction("Tasklist", "Tasklist", new { listId = task.ListId });
    }


    [HttpPost]
    public IActionResult Complete(int taskId) {
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        var task = _taskMethods.GetActiveTaskById(taskId);
        if (task == null || task.Status == TaskManager.Models.TaskStatus.Done) {
            return NotFound(); // Task doesn't exist or not in the required state
        }

        // Check if the user has access (Contributor, Admin, or Owner)
        var userRole = _listUserMethods.GetUserRoleInList(userId.Value, task.ListId);
        if (userRole == null) {
            return Forbid();
        }

        // Update task status to Done
        task.Status = TaskManager.Models.TaskStatus.Done;
        string errorMsg;
        if (!_taskMethods.UpdateTask(task, out errorMsg)){
            TempData["ErrorMessage"] = errorMsg;
            return RedirectToAction("Tasklist", "Tasklist", new { listId = task.ListId });
        }

        return RedirectToAction("Tasklist", "Tasklist", new { listId = task.ListId });
    }
}