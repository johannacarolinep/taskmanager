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
    // Create tasklist form
    ViewBag.listId = listId;
    return View();
    }


    [HttpPost]
    public IActionResult Create(TaskModel newTask) {
    
    if (!ModelState.IsValid) {
        return View(newTask);
    }

    string errorMsg;
    _taskMethods.AddTask(newTask, out errorMsg);
    if (string.IsNullOrEmpty(errorMsg)) {
        return RedirectToAction("Tasklist", "Tasklist", new { listId = newTask.ListId });
    }

    ModelState.AddModelError(string.Empty, errorMsg);
    return View(newTask);
    }

}