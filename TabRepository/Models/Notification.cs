using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.Models
{
    public class Notification
    {
        public int Id { get; set; }

        public string ToUserId { get; set; }

        public string FromUserId { get; set; }

        public int? ProjectId { get; set; }

        public string Title { get; set; }

        public string Message1 { get; set; }

        public string Message2 { get; set; }

        public DateTime Timestamp { get; set; }

        public NotificationType NotificationType { get; set; }

        public bool IsRead { get; set; }

        public virtual ApplicationUser ToUser { get; set; }

        public virtual ApplicationUser FromUser { get; set; }

        public virtual Project Project { get; set; }
    }

    public enum NotificationType
    {   // Do not rearrange - only add to bottom
        FriendRequested,
        FriendAccepted,
        ProjectAdded,
        ProjectDeleted,
        AlbumAdded,
        AlbumDeleted,
        TabAdded,
        TabDeleted,
        TabVersionAdded,
        TabVersionDeleted,
        ContributorAdded,
        InvoiceCreated,
        InvoiceUpdated,
        InvoicePaid,
        InvoicePaymentFailed,
        SubscriptionStatusUpdated,
        AccountTypeChanged,
        None
    }
}
