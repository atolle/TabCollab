using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TabRepository.Interfaces;

namespace TabRepository.Models
{
    public class Project : IItem
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        [Required]
        [Display(Name = "Project Name")]
        [StringLength(255)]
        public string Name { get; set; }

        public string Description { get; set; }

        public string ImageFileName { get; set; }

        public string ImageFilePath { get; set; }

        public DateTime DateCreated { get; set; }

        public DateTime DateModified { get; set; }

        public virtual ApplicationUser User { get; set; }

        public virtual ICollection<Album> Albums { get; set; } 
        
        public virtual ICollection<ProjectContributor> Contributors { get; set; }
    }
}