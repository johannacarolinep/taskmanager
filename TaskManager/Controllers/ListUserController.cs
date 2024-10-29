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
    public IActionResult Invite(int listId, string listTitle, string username) {
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
        var model = new InviteUserViewModel {
            ListId = listId,
            TasklistTitle = listTitle,
            InvitingUsername = username
        };

        Console.WriteLine("Before returning the form view");
        return View(model);
    }


    [HttpPost]
    public async Task<IActionResult> Invite(InviteUserViewModel model) {
        Console.WriteLine("Inside Task action");
        // Validate inputs
        if (!ModelState.IsValid) {
            return View(model);
        }

        // Make sure user is allowed to send invites
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        var userRole = _listUserMethods.GetUserRoleInList(userId.Value, model.ListId);
        if (userRole != UserListRole.Owner && userRole != UserListRole.Admin) {
            return Forbid(); // Only Owner/Admin can invite
        }

        // Retrieve invited user by email
        var inviteUser = await _userMethods.FindByEmailAsync(model.Email, CancellationToken.None);

        Console.WriteLine($"inviteUser: {inviteUser}");

        var newListUser = new ListUserModel {
            ListId = model.ListId,
            Role = model.Role,
            InvitationStatus = InvitationStatus.Pending,
            IsActive = false
        };

        Console.WriteLine("newListUser was created!!");

        // Check inviteUser's current relation to list
        if (inviteUser != null) {
            var inviteUserRole = _listUserMethods.GetUserRoleInList(inviteUser.Id, model.ListId);
            Console.WriteLine($"{inviteUser}'s role in the list is {inviteUserRole}");
            if (inviteUserRole != null) {
                ModelState.AddModelError(string.Empty, "This user is already a member of the list.");
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
            ? await SendInviteEmailExistingUser(model.Email, model.TasklistTitle, model.InvitingUsername) 
            : await SendInviteEmailNewUser(model.Email, model.TasklistTitle, model.InvitingUsername);
    
        if (!emailSent) {
            ModelState.AddModelError(string.Empty, "An invitation was created but the email failed to send. The invited user will be able to see the invitation in the UI.");
            return View(model); // Return to the view if email sending fails
        }

        TempData["SuccessMessage"] = "Invitation sent successfully!";
        return RedirectToAction("Tasklist", "Tasklist", new { listId = model.ListId });
    }

    private async Task<bool> SendInviteEmailExistingUser(string email, string listTitle, string InvitingUsername) {

        // http://localhost:5206/ListUser/Invitations
        // https://taskmanager-aeb6bqhrgubzadag.northeurope-01.azurewebsites.net/ListUser/Invitations

        string subject = "You were invited to a new tasklist in TaskManager";
        string plainText = $"You were invited to a new tasklist with name {listTitle} by TaskManager user {InvitingUsername}.\n\nClick here to see your invitations: https://taskmanager-aeb6bqhrgubzadag.northeurope-01.azurewebsites.net/ListUser/Invitations";
        string HtmlContent = $@"
            <h2>You've been invited to a new tasklist in TaskManager!</h2>
            <p>You were invited to a new tasklist: <strong>{listTitle}</strong>.</p>
            <p>You were invited to this list by <strong>{InvitingUsername}</strong></p>
            <p>Click the link below to view your invitations:</p>
            <p><a href='https://taskmanager-aeb6bqhrgubzadag.northeurope-01.azurewebsites.net/ListUser/Invitations' style='color: #1E90FF; text-decoration: none;'>View Invitations</a></p>
            <br />
            <p>Best regards,<br />The TaskManager Team</p>";

        if (await _emailService.SendEmailAsync(email, subject, plainText, HtmlContent)) {
            return true;
        }

        return false;
    }

    private async Task<bool> SendInviteEmailNewUser(string email, string listTitle, string InvitingUsername) {

        // https://taskmanager-aeb6bqhrgubzadag.northeurope-01.azurewebsites.net/User/Signup
        // http://localhost:5206/User/Signup

        string subject = "Register to see your new invitation in TaskManager";
        string plainText = $"You were invited by user {InvitingUsername} to a new tasklist with name {listTitle} in TaskManager.\n\nSign up for TaskManager today to see your invitation.\n\nClick here to register: https://taskmanager-aeb6bqhrgubzadag.northeurope-01.azurewebsites.net/User/Signup";
        string HtmlContent = $@"
            <h2>You've been invited to a new tasklist in TaskManager!</h2>
            <p>You were invited to a new tasklist: <strong>{listTitle}</strong>.</p>
            <p>You were invited to this list by <strong>{InvitingUsername}</strong></p>
            <p>TaskManager is a free productivity web app for collaborative todo-lists - perfect for keeping track of tasks at work/for school/for everyday life!</p>
            <p>Click the link below to register for a free account and see your pending invitation:</p>
            <p><a href='https://taskmanager-aeb6bqhrgubzadag.northeurope-01.azurewebsites.net/User/Signup' style='color: #1E90FF; text-decoration: none;'>Sign Up with TaskManager!</a></p>
            <br />
            <p>Best regards,<br />The TaskManager Team</p>";

        if (await _emailService.SendEmailAsync(email, subject, plainText, HtmlContent)) {
            return true;
        }

        return false;
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


    [HttpGet]
    public IActionResult LeaveList(int listId) {
        // Make sure user has permission to delete tasklist
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        // make sure user is not the owner of the tasklist
        var userRole = _listUserMethods.GetUserRoleInList(userId.Value, listId);
        if (userRole == UserListRole.Owner) {
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
    public IActionResult LeaveList(TasklistModel tasklist) {
        // Ensure the user has permission to delete the tasklist
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        // make sure user is not the owner of the tasklist
        var userRole = _listUserMethods.GetUserRoleInList(userId.Value, tasklist.Id);
        if (userRole == UserListRole.Owner) {
            return Forbid();
        }

        // Set the tasklist and its tasks as inactive. delete the listuser
        string errorMsg = "";
        if (_listUserMethods.DeleteListUserByUserAndList(userId.Value, tasklist.Id, out errorMsg)) {
            TempData["SuccessMessage"] = "You left the tasklist successfully.";
            return RedirectToAction("Tasklists", "Tasklist");
        }

        ModelState.AddModelError(string.Empty, errorMsg);
        return View(tasklist);
    }


    [HttpGet]
    public IActionResult UpdateRoles(int listId, string listTitle) {
        // Retrieve the current user's role to ensure they have permissions
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        var userRole = _listUserMethods.GetUserRoleInList(userId.Value, listId);
        if (userRole != UserListRole.Owner && userRole != UserListRole.Admin) {
            return Forbid(); // Only Owner or Admin can access this action
        }

        // Fetch contributors for the specified task list
        var contributors = _listUserMethods.GetContributorsByListId(listId);

        var owner = contributors.FirstOrDefault(c => c.Role == UserListRole.Owner.ToString());
        contributors.Remove(owner);

        // Map data to the UpdateRolesViewModel
        var viewModel = new UpdateRolesViewModel {
            ListId = listId,
            ListTitle = listTitle,
            CurrentUserRole = (UserListRole)userRole,
            ListUsers = contributors,
            Owner = owner
        };

        return View(viewModel);
    }


    [HttpPost]
    public IActionResult UpdateRoles(UpdateRolesViewModel model) {
        if (!ModelState.IsValid) {
            return View(model);
        }

        Console.WriteLine("Inside the update method");
        Console.WriteLine($"List title: {model.ListTitle}");

        // Retrieve the current user's ID
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        // Check if the user has permission to update roles
        var userRole = _listUserMethods.GetUserRoleInList(userId.Value, model.ListId);
        if (userRole != UserListRole.Owner && userRole != UserListRole.Admin) {
            return Forbid();
        }

        bool hasErrors = false;

        // Loop through the ListUsers and update their roles
        foreach (var contributor in model.ListUsers) {
            Console.WriteLine($"Contributor: {contributor.Username} should update to {contributor.Role}");
            // Update the role in the database
            string errorMsg;
            bool success = _listUserMethods.UpdateUserRole(contributor.ListUserId, contributor.Role, out errorMsg);

            if (!success) {
                // Handle error (e.g., log it, set a model error, etc.)
                ModelState.AddModelError(string.Empty, $"Failed to update role for user: {contributor.Username}");
                hasErrors = true;
            }
        }

        if(hasErrors) {
            return View(model);
        }

        TempData["SuccessMessage"] = "Roles updated successfully.";
        return RedirectToAction("Tasklist", "Tasklist", new { listid = model.ListId });
    }


    [HttpGet]
    public IActionResult TransferOwnership(int listId, string listTitle) {
        Console.WriteLine("Inside TranserOwnership");
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        // Check if the user is the owner of the list
        var userRole = _listUserMethods.GetUserRoleInList(userId.Value, listId);
        if (userRole != UserListRole.Owner) {
            return Forbid(); // Only the owner can transfer ownership
        }

        // Fetch contributors to populate the selection list (excluding the owner)
        var contributors = _listUserMethods.GetContributorsByListId(listId)
                            .Where(c => c.Role != UserListRole.Owner.ToString())
                            .ToList();

        // Map to the view model
        var viewModel = new TransferOwnershipViewModel {
            ListId = listId,
            ListTitle = listTitle,
            Contributors = contributors // Populate with all eligible users
        };

        return View(viewModel);
    }


    [HttpPost]
    public IActionResult TransferOwnership(TransferOwnershipViewModel model) {

        // Check if the current user is logged in and retrieve their user ID
        int? userId = User.GetUserId();
        if (!userId.HasValue) {
            return RedirectToAction("Login", "User");
        }

        // Ensure the current user is the owner of the list
        var currentRole = _listUserMethods.GetUserRoleInList(userId.Value, model.ListId);
        if (currentRole != UserListRole.Owner) {
            return Forbid();
        }

        // Validate that a new owner has been selected and that the transfer is confirmed
        if (model.NewOwnerId <= 0 || !model.ConfirmTransfer) {
            ModelState.AddModelError(string.Empty, "Please select a user to transfer ownership to and confirm the action.");
            // Repopulate the contributors list before returning the view
            model.Contributors = _listUserMethods.GetContributorsByListId(model.ListId)
                            .Where(c => c.Role != UserListRole.Owner.ToString())
                            .ToList();
            return View(model);
        }

        string errorMsg;

        // Update current owner's role to Admin
        if (!_listUserMethods.TransferOwnership(model.ListId, userId.Value, model.NewOwnerId, out errorMsg)) {
            ModelState.AddModelError(string.Empty, errorMsg);

            // Repopulate the contributors list before returning the view
            model.Contributors = _listUserMethods.GetContributorsByListId(model.ListId)
                            .Where(c => c.Role != UserListRole.Owner.ToString())
                            .ToList();
            return View(model);
        }

        // Success message and redirect
        TempData["SuccessMessage"] = $"Ownership has been successfully transferred.";
        return RedirectToAction("Tasklist", "Tasklist", new { listId = model.ListId });
    }
}