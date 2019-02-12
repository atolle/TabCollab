using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TabRepository.Models;

namespace TabRepository.ViewModels
{
    public class AlbumIndexViewModel
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        [Required(ErrorMessage = "Album Name is required")]
        [Display(Name = "Album Name")]
        [StringLength(255)]
        public string Name { get; set; }

        public string Owner { get; set; }

        public int ProjectId { get; set; }

        public string ProjectName { get; set; }

        public string ImageFileName { get; set; }

        public string ImageFilePath { get; set; }

        public DateTime DateCreated { get; set; }

        public DateTime DateModified { get; set; }

        public bool IsOwner { get; set; }

        public virtual ApplicationUser User { get; set; }

        public virtual ICollection<Tab> Tabs { get; set; }
    }
}
