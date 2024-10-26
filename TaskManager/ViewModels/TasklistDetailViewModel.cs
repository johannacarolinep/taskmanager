using System;
using System.Collections.Generic;
using TaskManager.Models;

namespace TaskManager.ViewModels;

    public class TasklistDetailViewModel {
        // Tasklist information
        public int TasklistId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedByUsername { get; set; }
        public string UserRole { get; set; }

        // Related tasks
        public List<TaskModel> Tasks { get; set; } = new List<TaskModel>();

        // Contributors information
        public List<ContributorInfo> Contributors { get; set; } = new List<ContributorInfo>();

        // Nested classes for structured data

        public class ContributorInfo {
            public int ListUserId { get; set; }
            public int UserId { get; set; }
            public string Username { get; set; }
            public string Image { get; set; }
            public string Role { get; set; }
        }
    }

