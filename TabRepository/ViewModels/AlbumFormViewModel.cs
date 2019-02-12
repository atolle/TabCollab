using TabRepository.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TabRepository.ViewModels
{
    public class AlbumFormViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Album Name is required")]
        [Display(Name = "Album Name")]
        [StringLength(255)]
        public string Name { get; set; }

        public string Description { get; set; }

        public IFormFile Image { get; set; }

        public int ProjectId { get; set; }

        public string ProjectName { get; set; }

        public AlbumFormViewModel()
        {
            Id = 0;
        }

        public AlbumFormViewModel(Album album)
        {
            Id = album.Id;
            Name = album.Name;
            Description = album.Description;
        }
    }
}