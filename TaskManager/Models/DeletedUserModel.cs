using System;

namespace TaskManager.Models;

public class DeletedUserModel {
    public int Id { get; set; }
    public required int UserId { get; set; }
    public required string EmailEncrypted { get; set; }
    public required string UserNameEncrypted { get; set; }
    public DateTime DeletionDate { get; set; } = DateTime.Now;

}

