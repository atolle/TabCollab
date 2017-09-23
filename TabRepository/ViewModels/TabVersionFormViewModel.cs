using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace TabRepository.ViewModels
{
    public class TabVersionFormViewModel
    {
        public int Id { get; set; }

        public int TabId { get; set; }

        public string TabName { get; set; }

        [Display(Name = "File")]
        public IFormFile FileData { get; set; }

        public string Description { get; set; }

        public TabVersionFormViewModel()
        {
            Id = 0;
        }
    }
}