using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.Models
{
    public class NotificationUser
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public int NotificationId { get; set; }

        public bool IsRead { get; set; }

        public virtual ApplicationUser User { get; set; }

        public virtual Notification Notification { get; set; }
    }
}
