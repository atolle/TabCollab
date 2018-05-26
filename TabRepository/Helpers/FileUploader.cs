using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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

        public async Task<string> UploadFileToFileSystem(IFormFile file, string userId, string folderId)
        {
            // Path to webroot\images\userId\folderId (i.e. webroot\images\1234\Project1)
            string relativePath = "\\images\\" + userId + "\\" + folderId;
            string userFolderPath = _appEnvironment.WebRootPath + relativePath;

            if (!Directory.Exists(userFolderPath))
            {
                Directory.CreateDirectory(userFolderPath);
            }

            string filePath = userFolderPath + "\\" + file.FileName;
            relativePath += "\\" + file.FileName;

            if (file.Length > 0)
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                    return relativePath.Replace("\\", "/");
                }
            }
            else
            {
                return "";
            }
        }
    }
}
