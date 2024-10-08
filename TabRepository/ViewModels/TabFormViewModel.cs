﻿using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TabRepository.ViewModels
{
    public class TabFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tab Name is required")]
        [Display(Name = "Tab Name")]
        [StringLength(255)]
        public string Name { get; set; }

        public IFormFile FileData { get; set; }

        [FileExtensions(Extensions = "gp,gpx,gp5,gp4,gp3,tg,txt,tbt,tab,btab,ptb,tef,nofile", ErrorMessage = "Invalid file type")]
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

        public int AlbumId { get; set; }

        public string AlbumName { get; set; }

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