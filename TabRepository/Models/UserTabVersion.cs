using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.Models
{
    public class UserTabVersion
    {
        public string UserId { get; set; }

        public int TabId { get; set; }

        public int Version { get; set; }

        public virtual ApplicationUser User { get; set; }

        public virtual Tab Tab { get; set; }
    }
}
