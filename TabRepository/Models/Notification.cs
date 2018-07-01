using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.Models
{
    public class Notification
    {
        public int Id { get; set; }

        public int ToUserId { get; set; }

        public int FromUserId { get; set; }

        public string Title { get; set; }

        public string Message { get; set; }

        public DateTime Timestamp { get; set; }

        public bool IsRead { get; set; }

        public virtual ApplicationUser ToUser { get; set; }

        public virtual ApplicationUser FromUser { get; set; }
    }
}
