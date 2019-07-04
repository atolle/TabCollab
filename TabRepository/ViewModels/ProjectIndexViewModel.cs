using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TabRepository.Models;

namespace TabRepository.ViewModels
{
    public class ProjectIndexViewModel 
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        [Required(ErrorMessage = "Project Name is required")]
        [Display(Name = "Project Name")]
        [StringLength(255)]
        public string Name { get; set; }

        public string Owner { get; set; }

        public string ImageFileName { get; set; }

        public string ImageFilePath { get; set; }

        public DateTime DateCreated { get; set; }

        public DateTime DateModified { get; set; }

        public bool IsOwner { get; set; }

        public bool AllowNewTabs { get; set; }

        public bool TabTutorialShown { get; set; }

        public bool SubscriptionExpired { get; set; }

        public DateTime? SubscriptionExpiration { get; set; }

        public virtual ApplicationUser User { get; set; }

        public virtual ICollection<Album> Albums { get; set; }        
    }
}
