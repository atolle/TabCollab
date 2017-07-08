using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using TabRepository;
using TabRepository.Data;
using TabRepository.Models;
using TabRespository.Models;
using TabRespository.ViewModels;

namespace TabRespository.Controllers
{
    [Authorize]
    public class TabsController : Controller
    {
        private ApplicationDbContext _context;

        public TabsController(ApplicationDbContext context)
        {
            _context = context;
        }

        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
        }

        public ActionResult New(int id)
        {
            string currentUserId = User.GetUserId();

            // Verify current user has access to this project
            var projectInDb = _context.Projects.Single(p => p.Id == id && p.UserId == currentUserId);
            if (projectInDb == null)
                return NotFound();

            var viewModel = new TabFormViewModel()
            {
                ProjectId = projectInDb.Id,
                ProjectName = projectInDb.Name
            };

            return View("TabForm", viewModel);
        }

        // POST: Tabs
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(TabFormViewModel viewModel)      
        {
            if (!ModelState.IsValid)    
            {
                return View("TabForm", viewModel);
            }

            if (viewModel.Id == 0)  // We are creating a new Tab
            {
                string currentUserId = User.GetUserId();

                TabFile tabFile = new TabFile();

                if (viewModel.FileData.Length > 0)
                {
                    using (var fileStream = viewModel.FileData.OpenReadStream())
                    using (var ms = new MemoryStream())
                    {
                        fileStream.CopyTo(ms);
                        var fileBytes = ms.ToArray();
                        tabFile.TabData = fileBytes;
                    }

                    tabFile.Name = viewModel.FileData.Name;
                    tabFile.DateCreated = DateTime.Now;
                }

                // Create new Tab
                Tab tab = new Tab()
                {
                    UserId = User.GetUserId(),
                    ProjectId = _context.Projects.Single(p => p.Id == viewModel.ProjectId && p.UserId == currentUserId).Id,
                    Name = viewModel.Name,
                    Description = viewModel.Description,
                    DateCreated = DateTime.Now,
                    DateModified = DateTime.Now,
                    CurrentVersion = 1
                };

                // Create first Tab Version
                TabVersion tabVersion = new TabVersion()
                {
                    Version = 1,                    // NEED TO DETERMINE HOW TO REFERENCE TABVERSION
                    Description = viewModel.Description,    // TO TABFILE AND VICE VERSA
                    UserId = tab.UserId,                    // CHICKEN AND THE EGG PROBLEM
                    DateCreated = tab.DateCreated,
                    TabId = tab.Id
                };

                //tabVersion.TabFileId = file.Id;

                _context.Tabs.Add(tab);
                _context.TabVersions.Add(tabVersion);
                _context.TabFiles.Add(tabFile);
            }

            _context.SaveChanges();

            // Redirect to list of tabs for current Project
            //return RedirectToAction("Index", "Tabs", new { id = viewModel.ProjectId });
            return RedirectToAction("Main", "Projects"); 
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxSave(TabFormViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                // Need to return JSON failure to form
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            try
            {
                if (viewModel.Id == 0)  // We are creating a new Tab
                {
                    string currentUserId = User.GetUserId();

                    TabFile tabFile = new TabFile();

                    if (viewModel.FileData.Length > 0)
                    {
                        using (var fileStream = viewModel.FileData.OpenReadStream())
                        using (var ms = new MemoryStream())
                        {
                            fileStream.CopyTo(ms);
                            var fileBytes = ms.ToArray();
                            tabFile.TabData = fileBytes;
                        }

                        tabFile.Name = viewModel.FileData.Name;
                        tabFile.DateCreated = DateTime.Now;
                    }

                    // Create new Tab
                    Tab tab = new Tab()
                    {
                        UserId = User.GetUserId(),
                        ProjectId = _context.Projects.Single(p => p.Id == viewModel.ProjectId && p.UserId == currentUserId).Id,
                        Name = viewModel.Name,
                        Description = viewModel.Description,
                        DateCreated = DateTime.Now,
                        DateModified = DateTime.Now,
                        CurrentVersion = 1
                    };

                    // Create first Tab Version
                    TabVersion tabVersion = new TabVersion()
                    {
                        Version = 1,                    // NEED TO DETERMINE HOW TO REFERENCE TABVERSION
                        Description = viewModel.Description,    // TO TABFILE AND VICE VERSA
                        UserId = tab.UserId,                    // CHICKEN AND THE EGG PROBLEM
                        DateCreated = tab.DateCreated,
                        TabId = tab.Id
                    };

                    //tabVersion.TabFileId = file.Id;

                    _context.Tabs.Add(tab);
                    _context.TabVersions.Add(tabVersion);
                    _context.TabFiles.Add(tabFile);

                    _context.SaveChanges();

                    // Redirect to list of tabs for current Project
                    //return RedirectToAction("Index", "Tabs", new { id = viewModel.ProjectId });
                    return RedirectToAction("UpdateTabVersionsTable", "TabVersions", new { id = tab.Id });
                }
                else
                {
                    // TO DO: Return correct table when editing a tab
                    return RedirectToAction("GetEmptyTabVersionsTable", "TabVersions", null);
                }
            }
            catch
            {
                // Need to return JSON failure to form
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            
        }

        // GET: Tabs
        public ActionResult Index(int id)
        {
            // Return a list of all Tabs belonging to the current user for current Project (id)
            string currentUserId = User.GetUserId();

            var viewModel = new TabIndexViewModel()
            {
                Tabs = _context.Tabs.Where(t => t.UserId == currentUserId && t.ProjectId == id).ToList(),
                ProjectName = _context.Projects.Single(p => p.Id == id).Name,
                ProjectId = id
            };

            return View(viewModel);
        }

        public ActionResult Delete(int id)
        {
            // Verify user has access to current Tab (id)
            string currentUserId = User.GetUserId();
            var tabInDb = _context.Tabs.SingleOrDefault(t => t.Id == id && t.UserId == currentUserId);

            if (tabInDb == null)
                return NotFound();

            // Get Project Id to return to view
            int projectId = _context.Tabs.Single(t => t.Id == id && t.UserId == currentUserId).ProjectId;

            _context.Tabs.Remove(tabInDb);
            _context.SaveChanges();

            //return RedirectToAction("Index", "Tabs", new { id = projectId }); 
            return RedirectToAction("Main", "Projects"); 
        }

        public ApplicationUser GetCurrentUser()
        {
            // Get current user to store as reference in Project table
            string currentUserId = User.GetUserId();

            return _context.Users.FirstOrDefault(u => u.Id == currentUserId);
        }

        [HttpGet]
        public ActionResult GetTabFormPartialView(int id)
        {
            string currentUserId = User.GetUserId();

            // Verify current user has access to this project
            var projectInDb = _context.Projects.Single(p => p.Id == id && p.UserId == currentUserId);
            if (projectInDb == null)
                return NotFound();

            var viewModel = new TabFormViewModel()
            {
                ProjectId = projectInDb.Id,
                ProjectName = projectInDb.Name
            };

            return PartialView("_TabForm", viewModel);
        }
    }
}