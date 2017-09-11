using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TabRepository.Data;
using TabRepository.Helpers;
using TabRepository.Models;
using TabRepository.ViewModels;

namespace TabRepository.Controllers
{
    [Authorize]
    public class AlbumsController : Controller
    {
        private ApplicationDbContext _context;
        private FileUploader _fileUploader;
        private readonly IHostingEnvironment _appEnvironment;

        public AlbumsController(ApplicationDbContext context, IHostingEnvironment appEnvironment)
        {
            _context = context;
            _appEnvironment = appEnvironment;
            _fileUploader = new FileUploader(context, appEnvironment);
        }

        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
        }

        public ActionResult New(int projectId)
        {
            string currentUserId = User.GetUserId();

            // Verify current user has access to this project
            var projectInDb = _context.Projects.Single(p => p.Id == projectId && p.UserId == currentUserId);
            if (projectInDb == null)
                return NotFound();

            var viewModel = new AlbumFormViewModel()
            {
                ProjectId = projectInDb.Id,
                ProjectName = projectInDb.Name
            };

            return View("AlbumForm", viewModel);
        }

        // POST: Projects
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(AlbumFormViewModel viewModel)
        {
            if (!ModelState.IsValid)    // If not valid, set the view model to current customer
            {                           // initialize membershiptypes and pass it back to same view
                return View("AlbumForm", viewModel);
            }

            if (viewModel.Id == 0)  // We are creating a new project
            {
                string currentUserId = User.GetUserId();

                // Saving properties for new Project
                Album album = new Album()
                {
                    UserId = User.GetUserId(),
                    Project = _context.Projects.Single(p => p.Id == viewModel.ProjectId && p.UserId == currentUserId),
                    Name = viewModel.Name,
                    Description = viewModel.Description,
                    DateCreated = DateTime.Now,
                    DateModified = DateTime.Now,
                };

                _context.Albums.Add(album);
            }

            _context.SaveChanges();

            return RedirectToAction("Main", "Projects");
        }

        // POST: Projects
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjaxSave(AlbumFormViewModel viewModel)
        {
            if (!ModelState.IsValid)    // If not valid, set the view model to current customer
            {                           // initialize membershiptypes and pass it back to same view
                // Need to return JSON failure to form
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            else
            {
                try
                {
                    if (viewModel.Id == 0)  // We are creating a new project
                    {
                        string currentUserId = User.GetUserId();

                        // Saving properties for new Project
                        Album album = new Album()
                        {
                            UserId = User.GetUserId(),
                            Project = _context.Projects.Single(p => p.Id == viewModel.ProjectId && p.UserId == currentUserId),
                            Name = viewModel.Name,
                            Description = viewModel.Description,
                            DateCreated = DateTime.Now,
                            DateModified = DateTime.Now,
                            ImageFileName = viewModel.Image.FileName
                        };

                        _context.Albums.Add(album);
                        _context.SaveChanges();

                        await _fileUploader.UploadFileToFileSystem(viewModel.Image, User.GetUserId(), "Album" + album.Id.ToString());

                        return Json(new { name = album.Name, id = album.Id });
                    }
                    else
                    {
                        // Handle edit of project
                        return Json(new { });
                    }
                }
                catch
                {
                    // Need to return failure to form
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }
            }
        }

        public ActionResult Delete(int id)
        {
            string currentUserId = User.GetUserId();

            var albumInDb = _context.Albums.SingleOrDefault(a => a.Id == id && a.UserId == currentUserId);

            // If current user does not have access to project or project does not exist
            if (albumInDb == null)
                return NotFound();

            _context.Albums.Remove(albumInDb);
            _context.SaveChanges();

            return RedirectToAction("Main", "Projects");
        }

        public ActionResult AjaxDelete(int id)
        {
            try
            {
                string currentUserId = User.GetUserId();

                var albumInDb = _context.Albums.SingleOrDefault(a => a.Id == id && a.UserId == currentUserId);

                // If current user does not have access to project or project does not exist
                if (albumInDb == null)
                    return NotFound();

                _context.Albums.Remove(albumInDb);
                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        // GET: Projects
        public ViewResult Index()
        {
            string currentUserId = User.GetUserId();
            List<AlbumIndexViewModel> viewModel = new List<AlbumIndexViewModel>();

            // Return a list of all Projects belonging to the current user
            var albums = _context.Albums.Include(u => u.User)
                .Include(a => a.Project)
                .Where(a => a.UserId == currentUserId)
                .OrderBy(a => a.Name)
                .ToList();

            foreach (var album in albums)
            {
                var elem = new AlbumIndexViewModel()
                {
                    Id = album.Id,
                    UserId = album.UserId,
                    Name = album.Name,
                    Owner = album.User.UserName,
                    ProjectName = album.Project.Name,
                    ImageFileName = album.ImageFileName,
                    ImageFilePath = "/images/" + album.UserId + "/Album" + album.Id + "/" + album.ImageFileName,
                    DateCreated = album.DateCreated,
                    DateModified = album.DateModified,
                    User = album.User,
                    Tabs = album.Tabs
                };

                // Add projects to project view model
                viewModel.Add(elem);
            }

            return View(viewModel);
        }

        public ApplicationUser GetCurrentUser()
        {
            string currentUserId = User.GetUserId();

            return _context.Users.FirstOrDefault(u => u.Id == currentUserId);
        }

        // GET: Album form
        [HttpGet]
        public ActionResult GetAlbumFormPartialView(int projectId)
        {
            string currentUserId = User.GetUserId();

            // Verify current user has access to this project
            var projectInDb = _context.Projects.Single(p => p.Id == projectId && p.UserId == currentUserId);
            if (projectInDb == null)
                return NotFound();

            var viewModel = new AlbumFormViewModel()
            {
                ProjectId = projectInDb.Id,
                ProjectName = projectInDb.Name
            };

            return PartialView("_AlbumForm", viewModel);
        }

        [HttpGet]
        public ActionResult GetAlbumListPartialView()
        {
            string currentUserId = User.GetUserId();
            List<AlbumIndexViewModel> viewModel = new List<AlbumIndexViewModel>();

            // Return a list of all Projects belonging to the current user
            var albums = _context.Albums.Include(u => u.User)
                .Include(a => a.Project)
                .Where(a => a.UserId == currentUserId)
                .OrderBy(a => a.Name)
                .ToList();

            foreach (var album in albums)
            {
                var elem = new AlbumIndexViewModel()
                {
                    Id = album.Id,
                    UserId = album.UserId,
                    Name = album.Name,
                    Owner = album.User.UserName,
                    ProjectName = album.Project.Name,
                    ImageFileName = album.ImageFileName,
                    ImageFilePath = "/images/" + album.UserId + "/Album" + album.Id + "/" + album.ImageFileName,
                    DateCreated = album.DateCreated,
                    DateModified = album.DateModified,
                    User = album.User,
                    Tabs = album.Tabs
                };

                // Add projects to project view model
                viewModel.Add(elem);
            }

            return PartialView("_AlbumList", viewModel);
        }
    }
}