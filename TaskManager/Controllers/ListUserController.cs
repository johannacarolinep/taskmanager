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

    private readonly EmailService _emailService;

    public ListUserController(TasklistMethods tasklistMethods, TaskMethods taskMethods, ListUserMethods listUserMethods, UserMethods userMethods, EmailService emailService) {
        _tasklistMethods = tasklistMethods;
        _taskMethods = taskMethods;
        _listUserMethods = listUserMethods;
        _userMethods = userMethods;
        _emailService = emailService;
    }


    [HttpGet]
    public IActionResult Invite(int listId) {
        // Ensure the user has permission to invite others
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        var userRole = _listUserMethods.GetUserRoleInList(userId.Value, listId);
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

        // Validate inputs
        if (!ModelState.IsValid) {
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
            Console.WriteLine($"{inviteUser}'s role in the list is {inviteUserRole}");
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
            ? await SendInviteEmailExistingUser(model.Email, listId) 
            : await SendInviteEmailNewUser(model.Email, listId);
    
        if (!emailSent) {
            ModelState.AddModelError(string.Empty, "Failed to send invitation email.");
            return View(model); // Return to the view if email sending fails
        }
        // need to FIX - check if email was actually sent in functions
        // pass in tasklist title and username to email functions
        TempData["SuccessMessage"] = "Invitation sent successfully!";
        return RedirectToAction("Tasklist", "Tasklist", new { listId = listId });
    }

    private async Task<bool> SendInviteEmailExistingUser(string email, int listId) {

        string subject = "You were invited to a new tasklist in TaskManager";
        string plainText = "You were invited to a new tasklist with name XXX.\n\nClick here to see your invitations: http://localhost:5206/ListUser/Invitations";
        string HtmlContent = @"
            <h2>You've been invited to a new tasklist in TaskManager!</h2>
            <p>You were invited to a new tasklist called <strong>XXX</strong>.</p>
            <p>Click the link below to view your invitations:</p>
            <p><a href='http://localhost:5206/ListUser/Invitations' style='color: #1E90FF; text-decoration: none;'>View Invitations</a></p>
            <br />
            <p>Best regards,<br />The TaskManager Team</p>";

        await _emailService.SendEmailAsync(email, subject, plainText, HtmlContent);

        return true; // return true if successful
    }

    // Sample method for sending email (you need to implement the actual logic)
    private async Task<bool> SendInviteEmailNewUser(string email, int listId) {
        string subject = "Register to see your new invitation in TaskManager";
        string plainText = "You were invited to a new tasklist with name XXX in TaskManager.\n\nSign up for TaskManager today to see your invitation.\n\nClick here to register: http://localhost:5206/User/Signup";
        string HtmlContent = @"
            <h2>You've been invited to a new tasklist in TaskManager!</h2>
            <p>You were invited to a new tasklist called <strong>XXX</strong>.</p>
            <br/>
            <p>TaskManager is a free productivity web app for collaborative todo-lists - perfect for keeping track of tasks at work/for school/for everyday life!</p>
            <p>Click the link below to register for a free account and see your pending invitation:</p>
            <p><a href='http://localhost:5206/User/Signup' style='color: #1E90FF; text-decoration: none;'>Sign Up with TaskManager!</a></p>
            <br />
            <p>Best regards,<br />The TaskManager Team</p>";

        await _emailService.SendEmailAsync(email, subject, plainText, HtmlContent);

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