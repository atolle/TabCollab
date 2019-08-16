using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.ViewModels
{
    public class BugReportFormViewModel
    {
        [Required(ErrorMessage = "Please describe what you saw")]
        [Display(Name = "What did you see?")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Page is required")]
        public string Page { get; set; }

        public List<SelectListItem> Pages { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Home Page", Text = "Home Page" },
            new SelectListItem { Value = "Tabs", Text = "Tabs" },
            new SelectListItem { Value = "Projects", Text = "Projects" },
            new SelectListItem { Value = "Albums", Text = "Albums" },
            new SelectListItem { Value = "Friends", Text = "Friends" },
            new SelectListItem { Value = "Search", Text = "Search" },
            new SelectListItem { Value = "Account", Text = "Account" },
            new SelectListItem { Value = "Other", Text = "Other" }
        };

        [Display(Name = "Errors")]
        public string Errors { get; set; }

        [Required(ErrorMessage = "Device Type is required")]
        [Display(Name = "Device Type")]
        public string DeviceType { get; set; }

        public List<SelectListItem> DeviceTypes { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Desktop Computer", Text = "Desktop Computer" },
            new SelectListItem { Value = "Laptop", Text = "Laptop" },
            new SelectListItem { Value = "Tablet", Text = "Tablet" },
            new SelectListItem { Value = "Phone", Text = "Phone" },
            new SelectListItem { Value = "Other", Text = "Other" }
        };

        [Required(ErrorMessage = "Browser is required")]
        public string Browser { get; set; }

        public List<SelectListItem> Browsers { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Chrome", Text = "Chrome" },
            new SelectListItem { Value = "Firefox", Text = "Firefox" },
            new SelectListItem { Value = "Safari", Text = "Safari" },
            new SelectListItem { Value = "Opera", Text = "Opera" },
            new SelectListItem { Value = "Internet Explorer", Text = "Internet Explorer" },
            new SelectListItem { Value = "Other", Text = "Other" }
        };

        public IFormFile Image { get; set; }

        public IFormFile CroppedImage { get; set; }

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
    }
}
