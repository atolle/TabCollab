using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TabRepository.Models;

namespace TabRespository.Models
{
    public class Tab
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public int ProjectId { get; set; }

        public string Description { get; set; }

        [Required]
        [Display(Name = "Tab Name")]
        [StringLength(255)]
        public string Name { get; set; }

        public int CurrentVersion { get; set; }

        public DateTime DateCreated { get; set; }

        public DateTime DateModified { get; set; }

        public virtual Project Project { get; set; }

        public virtual ApplicationUser User { get; set; }

        public virtual ICollection<TabVersion> TabVersions { get; set; }   
    }
}