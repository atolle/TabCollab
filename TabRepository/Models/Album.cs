﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TabRepository.Interfaces;

namespace TabRepository.Models
{
    public class Album : IItem
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public int ProjectId { get; set; }

        public string Description { get; set; }

        public string ImageFileName { get; set; }

        public string ImageFilePath { get; set; }

        [Required]
        [Display(Name = "Album Name")]
        [StringLength(255)]
        public string Name { get; set; }

        public int CurrentVersion { get; set; }

        public int Order { get; set; }

        public DateTime DateCreated { get; set; }

        public DateTime DateModified { get; set; }

        public virtual Project Project { get; set; }

        public virtual ApplicationUser User { get; set; }

        public virtual ICollection<Tab> Tabs { get; set; }
    }
}