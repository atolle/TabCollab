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

        public async Task UploadFileToFileSystem(IFormFile file, string userId, string folderId)
        {
            // Path to webroot\images\userId\folderId (i.e. webroot\images\1234\Project1)
            var userFolderPath = _appEnvironment.WebRootPath + "\\images\\" + userId + "\\" + folderId;

            if (!Directory.Exists(userFolderPath))
            {
                Directory.CreateDirectory(userFolderPath);
            }

            string filePath = userFolderPath + "\\" + file.FileName;

            if (file.Length > 0)
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }
        }
    }
}
