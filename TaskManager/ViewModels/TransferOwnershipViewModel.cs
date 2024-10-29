using System.Collections.Generic;
using TaskManager.Models;
using TaskManager.ViewModels;

namespace TaskManager.ViewModels
{
    public class TransferOwnershipViewModel
    {
        public int ListId { get; set; }
        public string ListTitle { get; set; }

        // List of contributors with their details
        public List<TasklistDetailViewModel.ContributorInfo> Contributors { get; set; } = new List<TasklistDetailViewModel.ContributorInfo>();
        public int NewOwnerId { get; set; }

        public bool ConfirmTransfer { get; set; }


    }
}