using TabRepository.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Collections;
using System.Collections.Generic;

namespace TabRepository.ViewModels
{
    public class ProjectFormViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Project Name is required")]
        [Display(Name = "Project Name")]
        [StringLength(255)]
        public string Name { get; set; }

        public string Description { get; set; }

        public IFormFile Image { get; set; }

        [FileExtensions(Extensions = "png,gif,jpeg,jpg,nofile", ErrorMessage = "Invalid file type")]
        public string FileName
        {
            get
            {
                if (Image != null)
                    return Image.FileName;
                else
                    return ".nofile";
            }
        }

        public IEnumerable<ApplicationUser> Friends { get; set; }

        public IList<UserViewModel> Contributors { get; set; }

        public ProjectFormViewModel()
        {
            Id = 0;
        }

        public ProjectFormViewModel(Project project)
        {
            Id = project.Id;
            Name = project.Name;
            Description = project.Description;
        }
    }
}