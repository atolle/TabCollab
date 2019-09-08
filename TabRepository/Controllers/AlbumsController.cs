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
        private UserAuthenticator _userAuthenticator;

        public AlbumsController(ApplicationDbContext context, IHostingEnvironment appEnvironment, UserAuthenticator userAuthenticator)
        {
            _context = context;
            _appEnvironment = appEnvironment;
            _fileUploader = new FileUploader(context, appEnvironment);
            _userAuthenticator = userAuthenticator;
        }

        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
        }

        public ActionResult New(int projectId)
        {
            string currentUserId = User.GetUserId();

            // Verify current user has access to this project
            var projectInDb = (Project)_userAuthenticator.CheckUserCreateAccess(Item.Project, projectId, currentUserId);

            if (projectInDb == null)
                return Json(new { error = "Project not found" });

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
            try
            {
                if (ModelState.IsValid)
                {
                    string currentUserId = User.GetUserId();
                    string currentUsername = User.GetUsername();
                    var userInDb = _context.Users.Where(u => u.Id == currentUserId).FirstOrDefault();

                    if (userInDb == null)
                    {
                        return Json(new { error = "User not found" });
                    }

                    // Verify current user has access to this project
                    var projectInDb = (Project)_userAuthenticator.CheckUserCreateAccess(Item.Project, viewModel.ProjectId, currentUserId);

                    if (projectInDb == null)
                    {
                        return Json(new { error = "Project not found" });
                    }

                    if (viewModel.Id == 0)  // We are creating a new album
                    {
                        using (var transaction = _context.Database.BeginTransaction())
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

                            if (viewModel.CroppedImage != null)
                            {
                                // Limit file size to 1 MB
                                if (viewModel.CroppedImage.Length > 1000000)
                                {
                                    return Json(new { error = "Image size limit is 1 MB" });
                                }

                                Helpers.File file = await _fileUploader.UploadFileToFileSystem(viewModel.CroppedImage, projectInDb.UserId, "Album" + album.Id.ToString());

                                album.ImageFilePath = file.Path;
                                album.ImageFileName = file.Name;
                            }

                            _context.Albums.Add(album);
                            _context.SaveChanges();

                            NotificationsController.AddNotification(_context, NotificationType.AlbumAdded, null, album.ProjectId, userInDb, album.Name, album.Project.Name);

                            transaction.Commit();

                            return Json(new { name = album.Name, id = album.Id });
                        }
                    }
                    else // We're updating an album
                    {
                        var albumInDb = (Album)_userAuthenticator.CheckUserEditAccess(Item.Album, viewModel.Id, currentUserId);

                        // If current user does not have access to project or project does not exist
                        if (albumInDb == null)
                        {
                            return Json(new { error = "Album not found" });
                        }

                        albumInDb.Name = viewModel.Name;
                        albumInDb.Description = viewModel.Description;
                        albumInDb.DateModified = DateTime.Now;

                        if (viewModel.CroppedImage != null)
                        {
                            // Limit file size to 1 MB
                            if (viewModel.CroppedImage.Length > 1000000)
                            {
                                return Json(new { error = "Image size limit is 1 MB" });
                            }

                            Helpers.File file = await _fileUploader.UploadFileToFileSystem(viewModel.CroppedImage, User.GetUserId(), "Album" + albumInDb.Id.ToString());

                            albumInDb.ImageFilePath = file.Path;
                            albumInDb.ImageFileName = file.Name;
                        }

                        _context.Albums.Update(albumInDb);
                        _context.SaveChanges();

                        return Json(new { name = albumInDb.Name, id = albumInDb.Id });
                    }
                }

                // If we got here there were errors in the modelstate                
                var modelErrors = ModelState.Values.SelectMany(v => v.Errors.Select(b => b.ErrorMessage)).ToList();

                return Json(new { error = string.Join("<br />", modelErrors) });
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }        
        }

        [HttpDelete]
        public ActionResult Delete(int id)
        {
            try
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    string currentUserId = User.GetUserId();
                    string currentUsername = User.GetUsername();
                    var userInDb = _context.Users.Where(u => u.Id == currentUserId).FirstOrDefault();

                    if (userInDb == null)
                    {
                        return Json(new { error = "User not found" });
                    }

                    var albumInDb = (Album)_userAuthenticator.CheckUserDeleteAccess(Item.Album, id, currentUserId);

                    // If current user does not have access to project or project does not exist
                    if (albumInDb == null)
                    {
                        return Json(new { error = "Album not found" });
                    }                        

                    _context.Albums.Remove(albumInDb);
                    _context.SaveChanges();

                    NotificationsController.AddNotification(_context, NotificationType.AlbumDeleted, null, albumInDb.ProjectId, userInDb, albumInDb.Name, albumInDb.Project.Name);

                    transaction.Commit();

                    return Json(new { success = true });
                }
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
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
                    var projectInDb = (Project)_userAuthenticator.CheckUserCreateAccess(Item.Project, projectId, currentUserId);

                    if (projectInDb == null)
                    {
                        return Json(new { error = "Project not found" });                       
                    }

                    viewModel.ProjectId = projectInDb.Id;
                    viewModel.ProjectName = projectInDb.Name;
                }
                // Existing album
                else
                {
                    var albumInDb = (Album)_userAuthenticator.CheckUserEditAccess(Item.Album, albumId, currentUserId);

                    if (albumInDb == null)
                    {
                        return Json(new { error = "Album not found" });
                    }

                    viewModel.Id = albumInDb.Id;
                    viewModel.Name = albumInDb.Name;
                    viewModel.Description = albumInDb.Description;
                }

                return PartialView("_AlbumForm", viewModel);
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            } 
        }

        [HttpGet]
        public ActionResult GetAlbumListPartialView(int projectId)
        {
            string currentUserId = User.GetUserId();
            List<AlbumIndexViewModel> viewModel = new List<AlbumIndexViewModel>();

            try
            {
                // Return a list of all Projects belonging to the current user
                var albums = _userAuthenticator.GetAllItems(Item.Album, projectId, currentUserId).Cast<Album>().OrderByDescending(a => a.UserId == currentUserId).ThenBy(a => a.Name).ToList();

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
                return Json(new { error = e.Message });
            }
        }

        [HttpGet]
        public ActionResult GetAlbumSelectionPartialView(int projectId)
        {
            try
            {
                string currentUserId = User.GetUserId();
                List<AlbumIndexViewModel> viewModel = new List<AlbumIndexViewModel>();

                var albums = _userAuthenticator.GetAllItems(Item.Album, projectId, currentUserId).Cast<Album>().ToList();

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

                return PartialView("_AlbumSelection", viewModel);
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
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
                    var albumInDb = (Album)_userAuthenticator.CheckUserEditAccess(Item.Album, albumIds[i], currentUserId);

                    if (albumInDb.UserId != currentUserId)
                    {
                        return Json(new { error = "Album not found" });
                    }

                    albumInDb.Order = i;
                }

                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }

        [HttpPost]
        public ActionResult MoveAlbum(int albumId, int projectId)
        {
            string currentUserId = User.GetUserId();

            try
            {
                var projectInDb = (Project)_userAuthenticator.CheckUserEditAccess(Item.Project, projectId, currentUserId);

                if (projectInDb == null)
                {
                    return Json(new { error = "Project not found" });
                }

                var albumInDb = (Album)_userAuthenticator.CheckUserEditAccess(Item.Album, albumId, currentUserId);

                if (albumInDb == null)
                {
                    return Json(new { error = "Album not found" });
                }

                albumInDb.Project = projectInDb;

                _context.SaveChanges();                

                return Json(new { success = true });
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }
    }
}