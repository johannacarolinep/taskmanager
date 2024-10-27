using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TaskManager.Models;
using System.Security.Claims;
using TaskManager.Models.Services;
using TaskManager.Helpers;
using TaskManager.ViewModels;

namespace TaskManager.Controllers;

[Authorize]
public class ListUserController : Controller {

    private readonly TasklistMethods _tasklistMethods;
    private readonly TaskMethods _taskMethods;
    private readonly ListUserMethods _listUserMethods;
    private readonly UserMethods _userMethods;

    public ListUserController(TasklistMethods tasklistMethods, TaskMethods taskMethods, ListUserMethods listUserMethods, UserMethods userMethods) {
        _tasklistMethods = tasklistMethods;
        _taskMethods = taskMethods;
        _listUserMethods = listUserMethods;
        _userMethods = userMethods;
    }


    [HttpGet]
    public IActionResult Invite(int listId) {
        // Ensure the user has permission to invite others
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        var userRole = _listUserMethods.GetUserRoleInList(listId, userId.Value);
        if (userRole != UserListRole.Owner && userRole != UserListRole.Admin) {
            return Forbid(); // Only Owner/Admin can invite
        }

        // Pass the listId to the view model
        ViewBag.ListId = listId;
        return View();
    }


    [HttpPost]
    public async Task<IActionResult> Invite(InviteUserViewModel model, int listId) {

        ViewBag.ListId = listId;
        Console.WriteLine($"List id : {listId}");

        // Validate inputs
        if (!ModelState.IsValid) {
            Console.WriteLine("Modelstats is invalid!");
            return View(model);
        }

        // Make sure user is allowed to send invites
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        var userRole = _listUserMethods.GetUserRoleInList(listId, userId.Value);
        if (userRole != UserListRole.Owner && userRole != UserListRole.Admin) {
            return Forbid(); // Only Owner/Admin can invite
        }

        Console.WriteLine($"Currentuser has role {userRole}");

        // Retrieve invited user by email
        var inviteUser = await _userMethods.FindByEmailAsync(model.Email, CancellationToken.None);

        Console.WriteLine($"inviteUser: {inviteUser}");

        var newListUser = new ListUserModel {
            ListId = listId,
            Role = model.Role,
            InvitationStatus = InvitationStatus.Pending,
            IsActive = false
        };

        Console.WriteLine("newListUser was created!!");

        // Check inviteUser's current relation to list
        if (inviteUser != null) {
            var inviteUserRole = _listUserMethods.GetUserRoleInList(inviteUser.Id, listId);
            if (inviteUserRole != null) {
                ModelState.AddModelError(string.Empty, "This user is already a member of the list.");
                ViewBag.ListId = listId;
                return View(model);
            }
            newListUser.UserId = inviteUser.Id;
        } else {
            newListUser.InviteEmail = model.Email;
        }

        Console.WriteLine("Before adding to database");

        // Add the ListUser to the database
        string errorMsg;
        _listUserMethods.AddListUser(newListUser, out errorMsg);

        Console.WriteLine("After adding to database!");

        if (!string.IsNullOrWhiteSpace(errorMsg)) {
            Console.WriteLine($"errorsMsg: {errorMsg}");
            ModelState.AddModelError(string.Empty, errorMsg);
            return View(model);
        }

        bool emailSent = inviteUser != null 
            ? SendInviteEmailExistingUser(model.Email, listId) 
            : SendInviteEmailNewUser(model.Email, listId);
    
        if (!emailSent) {
            ModelState.AddModelError(string.Empty, "Failed to send invitation email.");
            return View(model); // Return to the view if email sending fails
        }

        TempData["SuccessMessage"] = "Invitation sent successfully!";
        return RedirectToAction("Tasklist", "Tasklist", new { listId = listId });
    }

    // Sample method for sending email (you need to implement the actual logic)
    private bool SendInviteEmailExistingUser(string email, int listId) {
        // Your email sending logic here
        return true; // return true if successful
    }

    // Sample method for sending email (you need to implement the actual logic)
    private bool SendInviteEmailNewUser(string email, int listId) {
        // Your email sending logic here
        return true; // return true if successful
    }


    public IActionResult Invitations() {
        int? userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "User");
        }

        // Fetch pending invitations for the current user
        var tasklists = _listUserMethods.GetTasklistsWithPendingInvitations(userId.Value);

        return View(tasklists); // Pass tasklists directly to the view
    }


    [HttpPost]
    public IActionResult AcceptInvite(int listId) {
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        // Retrieve the ListUser object
        var listUser = _listUserMethods.GetListUserByListAndUser(listId, userId.Value);

        if (listUser == null) {
            // If the invite was not found, return an error or redirect
            TempData["ErrorMessage"] = "Invite not found or no pending invitation.";
            return RedirectToAction("Invitations");
        }

        // Update the invitation status and activation status
        listUser.InvitationStatus = InvitationStatus.Accepted;
        listUser.IsActive = true;

        // Save the changes
        string errorMsg;
        _listUserMethods.UpdateListUser(listUser, out errorMsg);

        if (string.IsNullOrEmpty(errorMsg)) {
            TempData["SuccessMessage"] = "Invitation accepted!";
            return RedirectToAction("Invitations");
        } else {
            TempData["ErrorMessage"] = errorMsg;
            return RedirectToAction("Invitations");
        }
    }


    [HttpPost]
    public IActionResult DeclineInvite(int listId) {
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        // Save the changes
        string errorMsg;
        if (_listUserMethods.DeleteListUserByUserAndList(userId.Value, listId, out errorMsg)) {
            TempData["SuccessMessage"] = "Invitation successfully declined!";
            return RedirectToAction("Invitations");
        }

        TempData["ErrorMessage"] = errorMsg;
        return RedirectToAction("Invitations");
    }

}