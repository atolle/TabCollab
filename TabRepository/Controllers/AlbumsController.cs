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


        // POST: Albums
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(AlbumFormViewModel viewModel)
        {
            if (!ModelState.IsValid)    
            {                           
                // Need to return JSON failure to form
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            try
            {
                string currentUserId = User.GetUserId();

                // Verify current user has access to this project
                var projectInDb = _context.Projects.SingleOrDefault(p => p.Id == viewModel.ProjectId && p.UserId == currentUserId);

                // If there is not project matching this project Id and this user Id, check to see if this user is a contributor
                if (projectInDb == null)
                {
                    projectInDb = (from project in _context.Projects
                                   join contributor in _context.ProjectContributors on project.Id equals contributor.ProjectId
                                   where contributor.UserId == currentUserId && project.Id == viewModel.ProjectId
                                   select project).Include(u => u.User).FirstOrDefault();

                    if (projectInDb == null)
                    {
                        return NotFound();
                    }
                }

                if (viewModel.Id == 0)  // We are creating a new album
                {                    
                    Album album = new Album()
                    {
                        UserId = projectInDb.UserId,
                        Project = projectInDb,
                        Name = viewModel.Name,
                        Description = viewModel.Description,
                        DateCreated = DateTime.Now,
                        DateModified = DateTime.Now
                    };

                    if (viewModel.Image != null)
                    {
                        album.ImageFileName = viewModel.Image.FileName;
                        string imageFilePath = await _fileUploader.UploadFileToFileSystem(viewModel.Image, projectInDb.UserId, "Album" + album.Id.ToString());
                        album.ImageFilePath = imageFilePath;
                    }

                    _context.Albums.Add(album);
                    _context.SaveChanges();                    

                    return Json(new { name = album.Name, id = album.Id });
                }
                else // We're updating an album
                {
                    var albumInDb = _context.Albums.SingleOrDefault(p => p.Id == viewModel.Id && p.UserId == currentUserId);

                    // If current user does not have access to project or project does not exist
                    if (albumInDb == null)
                    {
                        return NotFound();
                    }

                    albumInDb.Name = viewModel.Name;
                    albumInDb.Description = viewModel.Description;
                    albumInDb.DateModified = DateTime.Now;

                    if (viewModel.Image != null)
                    {
                        albumInDb.ImageFileName = viewModel.Image.FileName;
                        string imageFilePath = await _fileUploader.UploadFileToFileSystem(viewModel.Image, User.GetUserId(), "Album" + albumInDb.Id.ToString());
                        albumInDb.ImageFilePath = imageFilePath;
                    }

                    _context.Albums.Update(albumInDb);
                    _context.SaveChanges();

                    return Json(new { name = albumInDb.Name, id = albumInDb.Id });
                }
            }
            catch 
            {
                // Need to return failure to form
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }        
        }

        [HttpDelete]
        public ActionResult Delete(int id)
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
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        // GET: Albums
        public ViewResult Index()
        {
            return View();
        }

        public ApplicationUser GetCurrentUser()
        {
            string currentUserId = User.GetUserId();

            return _context.Users.FirstOrDefault(u => u.Id == currentUserId);
        }

        // GET: Album form
        // Two scenarios for this method:
        // projectId == 0 and albumId != 0 -> editing album
        // projectId != 0 and albumId == 0 -> creating new album
        [HttpGet]
        public ActionResult GetAlbumFormPartialView(int projectId, int albumId)
        {
           try
            {
                string currentUserId = User.GetUserId();
                var viewModel = new AlbumFormViewModel();

                // New album
                if (albumId == 0)
                {
                    // Verify current user has access to this project
                    var projectInDb = _context.Projects.SingleOrDefault(p => p.Id == projectId && p.UserId == currentUserId);

                    
                    if (projectInDb == null)
                    {
                        projectInDb = (from project in _context.Projects
                                        join contributor in _context.ProjectContributors on project.Id equals contributor.ProjectId                                        
                                        where contributor.UserId == currentUserId && project.Id == projectId
                                        select project).Include(u => u.User).FirstOrDefault();
                        
                        if (projectInDb == null)
                        {
                            return NotFound();
                        }                        
                    }

                    viewModel.ProjectId = projectInDb.Id;
                    viewModel.ProjectName = projectInDb.Name;
                }
                // Existing album
                else
                {
                    var albumInDb = _context.Albums.Single(p => p.Id == albumId && p.UserId == currentUserId);
                    if (albumInDb == null)
                    {
                        return NotFound();
                    }

                    viewModel.Id = albumInDb.Id;
                    viewModel.Name = albumInDb.Name;
                    viewModel.Description = albumInDb.Description;
                }

                return PartialView("_AlbumForm", viewModel);
            }
            catch (Exception e)
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            } 
        }

        [HttpGet]
        public ActionResult GetAlbumListPartialView(int projectId)
        {
            string currentUserId = User.GetUserId();
            List<AlbumIndexViewModel> viewModel = new List<AlbumIndexViewModel>();

            if (projectId == 0)
            {
                try
                {
                    // Return a list of all Albums belonging to the current user
                    var albums = _context.Albums.Include(u => u.User)
                        .Include(a => a.Project)
                        .Include(a => a.Tabs)
                        .Where(a => a.UserId == currentUserId)
                        .OrderBy(a => a.Project)
                        .ThenBy(a => a.Order)
                        .ToList();

                    var contributorAlbums = (from album in _context.Albums
                                             join contributor in _context.ProjectContributors on album.ProjectId equals contributor.ProjectId
                                             where contributor.UserId == currentUserId                                            
                                             select album).Include(u => u.User).Include(a => a.Project).Include(a => a.Tabs).ToList();

                    albums = albums.Union(contributorAlbums).ToList();

                    foreach (var album in albums)
                    {
                        var elem = new AlbumIndexViewModel()
                        {
                            Id = album.Id,
                            UserId = album.UserId,
                            Name = album.Name,
                            Owner = album.User.UserName,
                            ProjectId = album.Project.Id,
                            ProjectName = album.Project.Name,
                            ImageFileName = album.ImageFileName,
                            ImageFilePath = album.ImageFilePath,
                            DateCreated = album.DateCreated,
                            DateModified = album.DateModified,
                            User = album.User,
                            Tabs = album.Tabs,
                            IsOwner = album.UserId == currentUserId
                        };

                        // Add projects to project view model
                        viewModel.Add(elem);
                    }

                    return PartialView("_AlbumList", viewModel);
                }
                catch (Exception e)
                {
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }

            }
            else
            {
                try
                {
                    // Return a list of all Projects belonging to the current user
                    var albums = _context.Albums.Include(u => u.User)
                        .Include(a => a.Project)
                        .Where(a => a.UserId == currentUserId && a.ProjectId == projectId)
                        .OrderBy(a => a.Project)
                        .ThenBy(a => a.Order)
                        .ToList();

                    foreach (var album in albums)
                    {
                        var elem = new AlbumIndexViewModel()
                        {
                            Id = album.Id,
                            UserId = album.UserId,
                            Name = album.Name,
                            Owner = album.User.UserName,
                            ProjectId = album.Project.Id,
                            ProjectName = album.Project.Name,
                            ImageFileName = album.ImageFileName,
                            ImageFilePath = album.ImageFilePath,
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
                catch
                {
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }

            }
        }

        [HttpGet]
        public ActionResult GetAlbumSelectionPartialView(int projectId)
        {
            string currentUserId = User.GetUserId();
            List<AlbumIndexViewModel> viewModel = new List<AlbumIndexViewModel>();

            if (projectId == 0)
            {
                try
                {
                    // Return a list of all Albums belonging to the current user
                    var albums = _context.Albums.Include(u => u.User)
                        .Include(a => a.Project)
                        .Include(a => a.Tabs)
                        .Where(a => a.UserId == currentUserId)
                        .OrderBy(a => a.Project)
                        .ThenBy(a => a.Order)
                        .ToList();

                    var contributorAlbums = (from album in _context.Albums
                                             join contributor in _context.ProjectContributors on album.ProjectId equals contributor.ProjectId
                                             where contributor.UserId == currentUserId
                                             select album).Include(u => u.User).Include(a => a.Project).Include(a => a.Tabs).ToList();

                    albums = albums.Union(contributorAlbums).ToList();

                    foreach (var album in albums)
                    {
                        var elem = new AlbumIndexViewModel()
                        {
                            Id = album.Id,
                            UserId = album.UserId,
                            Name = album.Name,
                            Owner = album.User.UserName,
                            ProjectId = album.Project.Id,
                            ProjectName = album.Project.Name,
                            ImageFileName = album.ImageFileName,
                            ImageFilePath = album.ImageFilePath,
                            DateCreated = album.DateCreated,
                            DateModified = album.DateModified,
                            User = album.User,
                            Tabs = album.Tabs,
                            IsOwner = album.UserId == currentUserId
                        };

                        // Add projects to project view model
                        viewModel.Add(elem);
                    }

                    return PartialView("_AlbumList", viewModel);
                }
                catch (Exception e)
                {
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }

            }
            else
            {
                try
                {
                    // Return a list of all Projects belonging to the current user
                    var albums = _context.Albums.Include(u => u.User)
                        .Include(a => a.Project)
                        .Where(a => a.UserId == currentUserId && a.ProjectId == projectId)
                        .OrderBy(a => a.Project)
                        .ThenBy(a => a.Order)
                        .ToList();

                    foreach (var album in albums)
                    {
                        var elem = new AlbumIndexViewModel()
                        {
                            Id = album.Id,
                            UserId = album.UserId,
                            Name = album.Name,
                            Owner = album.User.UserName,
                            ProjectId = album.Project.Id,
                            ProjectName = album.Project.Name,
                            ImageFileName = album.ImageFileName,
                            ImageFilePath = album.ImageFilePath,
                            DateCreated = album.DateCreated,
                            DateModified = album.DateModified,
                            User = album.User,
                            Tabs = album.Tabs
                        };

                        // Add projects to project view model
                        viewModel.Add(elem);
                    }

                    return PartialView("_AlbumSelection", viewModel);
                }
                catch
                {
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }

            }
        }

        [HttpPost]
        public ActionResult ReorderAlbums(List<int> albumIds)
        {
            string currentUserId = User.GetUserId();

            try
            {
                for (int i = 0; i < albumIds.Count; i++)
                {
                    var album = _context.Albums.Where(a => a.Id == albumIds[i]).FirstOrDefault();

                    if (album.UserId != currentUserId)
                    {
                        return NotFound();
                    }

                    album.Order = i;
                }

                _context.SaveChanges();

                return Json(new { });
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        public ActionResult MoveAlbum(int albumId, int projectId)
        {
            string currentUserId = User.GetUserId();

            try
            {
                var project = _context.Projects.Where(p => p.Id == projectId && p.UserId == currentUserId).FirstOrDefault();

                if (project == null)
                {
                    return NotFound();
                }

                var album = _context.Albums.Where(a => a.Id == albumId && a.UserId == currentUserId).FirstOrDefault();

                if (album == null)
                {
                    return NotFound();
                }

                album.Project = project;

                _context.SaveChanges();                

                return Json(new { });
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}