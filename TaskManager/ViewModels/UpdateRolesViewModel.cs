using System.Collections.Generic;
using TaskManager.Models;
using TaskManager.ViewModels;

namespace TaskManager.ViewModels
{
    public class UpdateRolesViewModel
    {
        public int ListId { get; set; }
        public string ListTitle { get; set; }
        public UserListRole CurrentUserRole { get; set; } // Role of the user performing the update (e.g., "Admin" or "Owner")

        // List of contributors with their details
        public List<TasklistDetailViewModel.ContributorInfo> ListUsers { get; set; } = new List<TasklistDetailViewModel.ContributorInfo>();

        public TasklistDetailViewModel.ContributorInfo Owner { get; set; }
    }
}