using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TabRepository.Models;

namespace TabRepository.Dtos
{
    public class TabVersionDto
    {
        public int Id { get; set; }

        public int TabId { get; set; }

        public string UserId { get; set; }  // Contributor of this tab version, not necessarily the owner

        public string Description { get; set; }

        public int Version { get; set; }

        public DateTime DateCreated { get; set; }

        public bool IsOwner { get; set; }

        public TabFileDto TabFileDto { get; set; }
    }
}
