using System;
using TabRepository.Models;

namespace TabRespository.Models
{
    public class TabVersion
    {
        public int Id { get; set; }

        public int TabId { get; set; }

        public string UserId { get; set; }  // Contributor of this tab version, not necessarily the owner

        public string Description { get; set; }

        public int Version { get; set; }

        public DateTime DateCreated { get; set; }

        public virtual Tab Tab { get; set; }

        public virtual ApplicationUser User { get; set; }
        
        public virtual TabFile TabFile { get; set; }
    }
}