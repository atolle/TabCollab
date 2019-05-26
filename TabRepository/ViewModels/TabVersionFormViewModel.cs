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

        [FileExtensions(Extensions = "gp,gpx,gp5,gp4,gp3,tg,txt,tbt,nofile", ErrorMessage = "Invalid file type")]
        public string FileName
        {
            get
            {
                if (FileData != null)
                    return FileData.FileName;
                else if (Id != 0)
                    return ".nofile";
                else
                    return "";
            }
        }

        public string Description { get; set; }

        public TabVersionFormViewModel()
        {
            Id = 0;
        }
    }
}