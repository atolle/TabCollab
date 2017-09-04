using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using TabRepository.Data;

namespace TabRepository.Controllers
{
    [Authorize]
    public class TabFilesController : Controller
    {
        private ApplicationDbContext _context;

        public TabFilesController(ApplicationDbContext context)
        {
            _context = context;
        }

        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
        }

        // GET: TabFile
        // TODO: Add user validation to this method
        public FileResult Download(int id)
        {
            var file = _context.TabFiles.Single(f => f.Id == id);

            if (file == null)
            {
                // TO-DO: Handle this scenario
            }

            byte[] fileBytes = file.TabData;
            string fileName = file.Name;

            return File(fileBytes, "application/octet-stream", fileName);
        }

        // GET: TabFile
        // TODO: Add user validation to this method
        public ViewResult Player(int id)
        {
            ViewBag.Id = id.ToString();
            return View(); 
        }
    }
}