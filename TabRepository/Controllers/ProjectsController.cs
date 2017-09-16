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
        public async Task<IActionResult> Save(ProjectFormViewModel viewModel)
        {
            if (!ModelState.IsValid)    // If not valid, set the view model to current customer
            {                           // initialize membershiptypes and pass it back to same view
                // Need to return JSON failure to form
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            string currentUserId = User.GetUserId();

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
                else // We're updating a project
                {
                    var projectInDb = _context.Projects.SingleOrDefault(p => p.Id == viewModel.Id && p.UserId == currentUserId);

                    // If current user does not have access to project or project does not exist
                    if (projectInDb == null)
                    {
                        return NotFound();
                    }

                    projectInDb.Name = viewModel.Name;
                    projectInDb.Description = viewModel.Description;
                    projectInDb.DateModified = DateTime.Now;

                    if (viewModel.Image != null)
                    {
                        projectInDb.ImageFileName = viewModel.Image.FileName;
                        await _fileUploader.UploadFileToFileSystem(viewModel.Image, User.GetUserId(), "Project" + projectInDb.Id.ToString());
                    }

                    _context.Projects.Update(projectInDb);
                    _context.SaveChanges();

                    return Json(new { name = projectInDb.Name, id = projectInDb.Id });           
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
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
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

        public ViewResult Main()
        {
            string currentUserId = User.GetUserId();

            // Return a list of all Projects belonging to the current user
            var projects = _context.Projects
                .Include(p => p.Albums)    
                .ThenInclude(a => a.Tabs)
                .Where(p => p.UserId == currentUserId).ToList();

            return View(projects);
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
                    var projectInDb = _context.Projects.SingleOrDefault(p => p.UserId == currentUserId && p.Id == id);
                    var viewModel = new ProjectFormViewModel()
                    {
                        Id = projectInDb.Id,
                        Name = projectInDb.Name,
                        Description = projectInDb.Description
                    };

                    return PartialView("_ProjectForm", viewModel);
                }
                catch
                {
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }
            }
            else
            {
                var viewModel = new ProjectFormViewModel();

                return PartialView("_ProjectForm", viewModel);
            }
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
                    Albums = proj.Albums
                };

                // Add projects to project view model
                viewModel.Add(elem);
            }

            return PartialView("_ProjectList", viewModel);
        }
    }
}