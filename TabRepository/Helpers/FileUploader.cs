﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using TabRepository.Data;

namespace TabRepository.Helpers
{
    public class FileUploader
    {
        private ApplicationDbContext _context;
        private readonly IHostingEnvironment _appEnvironment;

        public FileUploader(ApplicationDbContext context, IHostingEnvironment appEnvironment)
        {
            _context = context;
            _appEnvironment = appEnvironment;
        }

        public async Task<File> UploadFileToFileSystem(IFormFile file, string userId, string folderId)
        {
            // Path to webroot\images\userId\folderId (i.e. webroot\images\1234\Project1)
            string relativePath = "\\images\\" + userId + "\\" + folderId;
            string userFolderPath = _appEnvironment.WebRootPath + relativePath;

            if (!Directory.Exists(userFolderPath))
            {
                Directory.CreateDirectory(userFolderPath);
            }

            string fileName = generateFileName(file);

            string filePath = userFolderPath + "\\" + fileName;
            relativePath += "\\" + fileName;

            if (file.Length > 0)
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                    return new File(fileName, relativePath.Replace("\\", "/"));
                }
            }
            else
            {
                return new File("", "");
            }
        }

        private string generateFileName(IFormFile file)
        {
            return (Guid.NewGuid()).ToString() + Path.GetExtension(file.FileName);
        }
    }

    public class File
    {
        public string Name { get; set; }
        public string Path { get; set; }

        public File(string name, string path)
        {
            Name = name;
            Path = path;
        }
    }
}
