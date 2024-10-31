using System;
using System.ComponentModel.DataAnnotations;

namespace TaskManager.Models;
    public class TaskModel {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ListId { get; set; } // Foreign key to Tasklist

        [Required]
        [MaxLength(100)]
        public string Description { get; set; } = "Unnamed task";

        [Required]
        [Range(1, 3)]
        public byte Priority { get; set; }

        [Required]
        public TaskStatus Status { get; set; } = TaskStatus.NotStarted;

        [Required]
        public DateTime Deadline { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public bool IsActive { get; set; } = true;
    }

    public enum TaskStatus {
        [Display(Name = "Not started")]
        NotStarted,

        [Display(Name = "In progress")]
        InProgress,

        [Display(Name = "Done")]
        Done
    }
