using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TabRepository.ViewModels
{
    public class TabFormViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Tab Name")]
        [StringLength(255)]
        public string Name { get; set; }

        [Required]
        public IFormFile FileData { get; set; }

        public string Description { get; set; }

        public int ProjectId { get; set; }

        public string ProjectName { get; set; }

        public TabFormViewModel()
        {
            Id = 0;
        }

        //public TabFormViewModel(Tab tab)
        //{
        //    Id = tab.Id;
        //    Name = tab.Name;
        //    Description = tab.Description;
        //}
    }
}