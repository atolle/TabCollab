using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TabRepository.Models;

namespace TabRepository.ViewModels
{
    public class TabIndexViewModel
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        [Required(ErrorMessage = "Tab Name is required")]
        [Display(Name = "Tab Name")]
        [StringLength(255)]
        public string Name { get; set; }

        public string Contributor { get; set; }

        public int AlbumId { get; set; }

        public string AlbumName { get; set; }

        public DateTime DateCreated { get; set; }

        public DateTime DateModified { get; set; }

        public bool IsOwner { get; set; }

        public virtual ApplicationUser User { get; set; }

        public virtual ICollection<TabVersion> TabVersions { get; set; }
    }
}