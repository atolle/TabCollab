using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.Models
{
    public class ProjectContributor
    {
        [Key, ForeignKey("User")]
        public string UserId { get; set; }

        [Key, ForeignKey("Project")]
        public int ProjectId { get; set; }

        public virtual ApplicationUser User { get; set; }

        public virtual Project Project { get; set; }
    }
}
