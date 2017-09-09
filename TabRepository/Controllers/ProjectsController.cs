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

        public ProjectsController(ApplicationDbContext context, IHostingEnvironment appEnvironment)
        {
            _context = context;
            _appEnvironment = appEnvironment;
            _fileUploader = new FileUploader(context, appEnvironment);  
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
        public ActionResult Save(ProjectFormViewModel viewModel)
        {
            if (!ModelState.IsValid)    // If not valid, set the view model to current customer
            {                           // initialize membershiptypes and pass it back to same view
                return View("ProjectForm", viewModel);
            }

            if (viewModel.Id == 0)  // We are creating a new project
            {
                // Saving properties for new Project
                Project project = new Project()
                {
                    UserId = User.GetUserId(),
                    Name = viewModel.Name,
                    Description = viewModel.Description,
                    DateCreated = DateTime.Now,
                    DateModified = DateTime.Now,
                };

                _context.Projects.Add(project);
            }

            _context.SaveChanges();

            return RedirectToAction("Main", "Projects");
        }

        // POST: Projects
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjaxSave(ProjectFormViewModel viewModel)
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
                        // Saving properties for new Project
                        Project project = new Project()
                        {
                            UserId = User.GetUserId(),
                            Name = viewModel.Name,
                            Description = viewModel.Description,
                            DateCreated = DateTime.Now,
                            DateModified = DateTime.Now,
                            ImageFileName = viewModel.Image.FileName
                        };

                        _context.Projects.Add(project);
                        _context.SaveChanges();

                        await _fileUploader.UploadFileToFileSystem(viewModel.Image, User.GetUserId(), "Project" + project.Id.ToString());

                        return Json(new { name = project.Name, id = project.Id });
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

            var projectInDb = _context.Projects.SingleOrDefault(p => p.Id == id && p.UserId == currentUserId);

            // If current user does not have access to project or project does not exist
            if (projectInDb == null)
                return NotFound();

            _context.Projects.Remove(projectInDb);
            _context.SaveChanges();

            return RedirectToAction("Main", "Projects");
        }

        public ActionResult AjaxDelete(int id)
        {
            try
            {
                string currentUserId = User.GetUserId();

                var projectInDb = _context.Projects.SingleOrDefault(p => p.Id == id && p.UserId == currentUserId);

                // If current user does not have access to project or project does not exist
                if (projectInDb == null)
                    return NotFound();

                _context.Projects.Remove(projectInDb);
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
            List<ProjectIndexViewModel> viewModel = new List<ProjectIndexViewModel>();

            // Return a list of all Projects belonging to the current user
            var projects = _context.Projects.Include(u => u.User)
                .Where(p => p.UserId == currentUserId)
                .OrderBy(p => p.Name)
                .ToList();

            foreach (var proj in projects)
            {
                var elem = new ProjectIndexViewModel()
                {
                    Id = proj.Id,
                    UserId = proj.UserId,
                    Name = proj.Name,
                    Owner = proj.User.UserName,
                    ImageFileName = proj.ImageFileName,
                    ImageFilePath = "/images/" + proj.UserId + "/Project" + proj.Id + "/" + proj.ImageFileName,
                    DateCreated = proj.DateCreated,
                    DateModified = proj.DateModified,
                    User = proj.User,
                    Tabs = proj.Tabs
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

        public ViewResult Main()
        {
            string currentUserId = User.GetUserId();

            // Return a list of all Projects belonging to the current user
            var projects = _context.Projects
                .Include(p => p.Tabs)
                .Where(p => p.UserId == currentUserId).ToList();

            return View(projects);
        }

        //// Needed for tab playback - should probably move to a different controller
        //public ActionResult DefaultSoundFont()
        //{
        //    string filename = "default.sf2";
        //    string filepath = AppDomain.CurrentDomain.BaseDirectory + "/Content/" + filename;
        //    byte[] filedata = System.IO.File.ReadAllBytes(filepath);
        //    string contentType = MimeMapping.GetMimeMapping(filepath);

        //    var cd = new System.Net.Mime.ContentDisposition
        //    {
        //        FileName = filename,
        //        Inline = true,
        //    };

        //    return File(filedata, contentType);
        //}

        // GET: Project form
        [HttpGet]
        public ActionResult GetProjectFormPartialView()
        {
            var viewModel = new ProjectFormViewModel();

            return PartialView("_ProjectForm", viewModel);
        }

        [HttpGet]
        public ActionResult GetProjectListPartialView()
        {
            string currentUserId = User.GetUserId();
            List<ProjectIndexViewModel> viewModel = new List<ProjectIndexViewModel>();

            // Return a list of all Projects belonging to the current user
            var projects = _context.Projects.Include(u => u.User)
                .Where(p => p.UserId == currentUserId)
                .OrderBy(p => p.Name)
                .ToList();

            foreach (var proj in projects)
            {
                var elem = new ProjectIndexViewModel()
                {
                    Id = proj.Id,
                    UserId = proj.UserId,
                    Name = proj.Name,
                    Owner = proj.User.UserName,
                    ImageFileName = proj.ImageFileName,
                    ImageFilePath = "/images/" + proj.UserId + "/Project" + proj.Id + "/" + proj.ImageFileName,
                    DateCreated = proj.DateCreated,
                    DateModified = proj.DateModified,
                    User = proj.User,
                    Tabs = proj.Tabs
                };

                // Add projects to project view model
                viewModel.Add(elem);
            }

            return PartialView("_ProjectList", viewModel);
        }
    }
}