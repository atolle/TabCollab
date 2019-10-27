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
    public class ProjectsController : Controller
    {
        private ApplicationDbContext _context;
        private FileUploader _fileUploader;
        private readonly IHostingEnvironment _appEnvironment;
        private UserAuthenticator _userAuthenticator;

        public ProjectsController(ApplicationDbContext context, IHostingEnvironment appEnvironment, UserAuthenticator userAuthenticator)
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

        public ActionResult New()
        {
            var viewModel = new ProjectFormViewModel();

            return View("ProjectForm", viewModel);
        }

        // POST: Projects
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(ProjectFormViewModel viewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    string currentUserId = User.GetUserId();
                    string currentUsername = User.GetUsername();
                    var userInDb = _context.Users.Where(u => u.Id == currentUserId).FirstOrDefault();

                    if (viewModel.Id == 0)  // We are creating a new project
                    {
                        using (var transaction = _context.Database.BeginTransaction())
                        {
                            // Saving properties for new Project
                            Project project = new Project()
                            {
                                UserId = User.GetUserId(),
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

                                Helpers.File file = await _fileUploader.UploadFileToFileSystem(viewModel.CroppedImage, User.GetUserId(), "Project" + project.Id.ToString());

                                project.ImageFilePath = file.Path;
                                project.ImageFileName = file.Name;
                            }

                            _context.Projects.Add(project);
                            _context.SaveChanges();

                            // Add contributors
                            if (viewModel.Contributors != null)
                            {
                                if (userInDb.AccountType == Models.AccountViewModels.AccountType.Free && viewModel.Contributors.Count > 2)
                                {
                                    return Json(new { error = "You can only add up to 2 contributors per Project." });
                                }
                                else if (userInDb.AccountType == Models.AccountViewModels.AccountType.Pro && viewModel.Contributors.Count > 6)
                                {
                                    return Json(new { error = "You can only add up to 6 contributors per Project." });
                                }

                                foreach (UserViewModel user in viewModel.Contributors)
                                {
                                    ProjectContributor contributor = new ProjectContributor()
                                    {
                                        UserId = _context.Users.Where(u => u.UserName == user.Username).Select(u => u.Id).FirstOrDefault(),
                                        ProjectId = project.Id
                                    };

                                    _context.ProjectContributors.Add(contributor);
                                }
                            }

                            _context.SaveChanges();

                            transaction.Commit();

                            return Json(new { name = project.Name, id = project.Id, imageFilePath = project.ImageFilePath == null ? "/images/TabCollab_icon_white_square_512.png" : project.ImageFilePath });
                        }
                    }
                    else // We're updating a project
                    {
                        using (var transaction = _context.Database.BeginTransaction())
                        {
                            var projectInDb = (Project)_userAuthenticator.CheckUserEditAccess(Item.Project, viewModel.Id, currentUserId);

                            // If current user does not have access to project or project does not exist
                            if (projectInDb == null)
                            {
                                return Json(new { error = "Project not found" });
                            }

                            projectInDb.Name = viewModel.Name;
                            projectInDb.Description = viewModel.Description;
                            projectInDb.DateModified = DateTime.Now;

                            if (viewModel.CroppedImage != null)
                            {
                                // Limit file size to 1 MB
                                if (viewModel.CroppedImage.Length > 1000000)
                                {
                                    return Json(new { error = "Image size limit is 1 MB" });
                                }

                                Helpers.File file = await _fileUploader.UploadFileToFileSystem(viewModel.CroppedImage, User.GetUserId(), "Project" + projectInDb.Id.ToString());

                                projectInDb.ImageFilePath = file.Path;
                                projectInDb.ImageFileName = file.Name;
                            }

                            // Add new contributors, remove any that were removed
                            var contributors = _context.ProjectContributors.Where(c => c.ProjectId == projectInDb.Id).ToList();

                            if (viewModel.Contributors != null)
                            {
                                if (userInDb.AccountType == Models.AccountViewModels.AccountType.Free && viewModel.Contributors.Count > 2)
                                {
                                    return Json(new { error = "You can only add up to 2 contributors per Project." });
                                }
                                else if (userInDb.AccountType == Models.AccountViewModels.AccountType.Pro && viewModel.Contributors.Count > 6)
                                {
                                    return Json(new { error = "You can only add up to 6 contributors per Project." });
                                }

                                foreach (UserViewModel user in viewModel.Contributors)
                                {
                                    var userId = _context.Users.Where(u => u.UserName == user.Username).Select(u => u.Id).FirstOrDefault();

                                    // Skip this contributor if they're already added
                                    if (contributors.Any(c => c.UserId == userId))
                                    {
                                        // Remove them from the list
                                        contributors = contributors.Where(u => u.UserId != userId).ToList();
                                        continue;
                                    }
                                    else
                                    {
                                        ProjectContributor contributor = new ProjectContributor()
                                        {
                                            UserId = _context.Users.Where(u => u.UserName == user.Username).Select(u => u.Id).FirstOrDefault(),
                                            ProjectId = projectInDb.Id
                                        };

                                        _context.ProjectContributors.Add(contributor);

                                        NotificationsController.AddNotification(_context, NotificationType.ContributorAdded, null, projectInDb.Id, userInDb, user.Username, projectInDb.Name);
                                    }
                                }
                            }

                            // Remove any contributors who did not come over from the view
                            foreach (ProjectContributor contributor in contributors)
                            {
                                _context.ProjectContributors.Remove(contributor);
                            }

                            _context.Projects.Update(projectInDb);
                            _context.SaveChanges();

                            transaction.Commit();

                            return Json(new { name = projectInDb.Name, id = projectInDb.Id, imageFilePath = projectInDb.ImageFilePath == null ? "/images/TabCollab_icon_white_square_512.png" : projectInDb.ImageFilePath });
                        }
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
                string currentUserId = User.GetUserId();

                // Since we're removing a project, we need to remove its contributor records first
                var contributorsInDb = _context.ProjectContributors.Where(c => c.ProjectId == id).ToList();

                if (contributorsInDb != null)
                {
                    foreach (ProjectContributor contributor in contributorsInDb)
                    {
                        _context.ProjectContributors.Remove(contributor);
                    }
                }

                var projectInDb = (Project)_userAuthenticator.CheckUserDeleteAccess(Item.Project, id, currentUserId);

                // If current user does not have access to project or project does not exist
                if (projectInDb == null)
                    return Json(new { error = "Project not found" });

                var notificationsInDb = _context.Notifications.Where(n => n.ProjectId == projectInDb.Id).ToList();

                if (notificationsInDb != null)
                {
                    foreach (Notification notification in notificationsInDb)
                    {
                        _context.Notifications.Remove(notification);
                    }
                }

                _context.Projects.Remove(projectInDb);
                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }

        // GET: Projects
        public ViewResult Index()
        {
            return View();
        }

        public ApplicationUser GetCurrentUser()
        {
            string currentUserId = User.GetUserId();

            return _context.Users.FirstOrDefault(u => u.Id == currentUserId);
        }

        [HttpGet]
        public ActionResult GetProjectFormPartialView(int id)
        {
            string currentUserId = User.GetUserId();
            if (id != 0)
            {
                try
                {
                    // Return a list of all Projects belonging to the current user
                    var projectInDb = (Project)_userAuthenticator.CheckUserEditAccess(Item.Project, id, currentUserId);

                    if (projectInDb == null)
                        return Json(new { error = "Project not found" });

                    var contributors = _context.ProjectContributors
                        .Where(p => p.ProjectId == projectInDb.Id)
                        .Select(p => new UserViewModel { Username = p.User.UserName, FirstName =  p.User.FirstName, LastName = p.User.LastName }).ToList();

                    var friends = _context.Friends
                        .Where(f => (f.User1Id == currentUserId || f.User2Id == currentUserId) && f.Status == FriendStatus.Friends)
                        .Select(f => f.User1Id == currentUserId ? f.User2 : f.User1).ToList();


                    var viewModel = new ProjectFormViewModel()
                    {
                        Id = projectInDb.Id,
                        Name = projectInDb.Name,
                        Description = projectInDb.Description,
                        Contributors = contributors,
                        Friends = friends
                    };

                    return PartialView("_ProjectForm", viewModel);
                }
                catch (Exception e)
                {
                    return Json(new { error = e.Message });
                }
            }
            else
            {
                try
                {
                    var friends = _context.Friends
                        .Where(f => (f.User1Id == currentUserId || f.User2Id == currentUserId) && f.Status == FriendStatus.Friends)
                        .Select(f => f.User1Id == currentUserId ? f.User2 : f.User1).ToList();

                    var viewModel = new ProjectFormViewModel()
                    {
                        Friends = friends
                    };

                    return PartialView("_ProjectForm", viewModel);
                }
                catch (Exception e)
                {
                    return Json(new { error = e.Message });
                }
            }
        }

        [HttpGet]
        public ActionResult GetProjectListPartialView()
        {
            try
            {
                string currentUserId = User.GetUserId();
                List<ProjectIndexViewModel> viewModel = new List<ProjectIndexViewModel>();

                // Return a list of all Projects belonging to the current user
                var projects = _userAuthenticator.GetAllItems(Item.Project, null, currentUserId).Cast<Project>().OrderByDescending(p => p.UserId == currentUserId).ThenBy(p => p.Name).ToList();

                foreach (var project in projects)
                {
                    var vm = new ProjectIndexViewModel()
                    {
                        Id = project.Id,
                        UserId = project.UserId,
                        Name = project.Name,
                        Owner = project.User.UserName,
                        ImageFileName = project.ImageFileName,
                        ImageFilePath = project.ImageFilePath,
                        DateCreated = project.DateCreated,
                        DateModified = project.DateModified,
                        User = project.User,
                        Albums = project.Albums,
                        IsOwner = project.UserId == currentUserId
                    };

                    // Add projects to project view model
                    viewModel.Add(vm);
                }

                return PartialView("_ProjectList", viewModel);
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }

        [HttpGet]
        public ActionResult GetProjectSelectionPartialView()
        {
            try
            {
                string currentUserId = User.GetUserId();
                List<ProjectIndexViewModel> viewModel = new List<ProjectIndexViewModel>();

                // Return a list of all Projects belonging to the current user
                var projects = _userAuthenticator.GetAllItems(Item.Project, null, currentUserId).Cast<Project>().ToList();

                foreach (var proj in projects)
                {
                    var vm = new ProjectIndexViewModel()
                    {
                        Id = proj.Id,
                        UserId = proj.UserId,
                        Name = proj.Name,
                        Owner = proj.User.UserName,
                        ImageFileName = proj.ImageFileName,
                        ImageFilePath = proj.ImageFilePath,
                        DateCreated = proj.DateCreated,
                        DateModified = proj.DateModified,
                        User = proj.User,
                        Albums = proj.Albums,
                        IsOwner = proj.UserId == currentUserId
                    };

                    // Add projects to project view model
                    viewModel.Add(vm);
                }

                return PartialView("_ProjectSelection", viewModel);
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }
    }
}