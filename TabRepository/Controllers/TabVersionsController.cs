using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TabRepository.Data;
using TabRepository.Models;
using TabRepository.ViewModels;

namespace TabRepository.Controllers
{
    [Authorize]
    public class TabVersionsController : Controller
    {
        private ApplicationDbContext _context;

        public TabVersionsController(ApplicationDbContext context)
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

            // Verify current user has access to this Tab
            var tabInDb = _context.Tabs.Single(t => t.Id == id && t.UserId == currentUserId);

            if (tabInDb == null)
                return NotFound();

            var viewModel = new TabVersionFormViewModel()
            {
                TabId = id,
                TabName = _context.Tabs.Single(t => t.Id == id).Name
            };

            return View("TabVersionForm", viewModel);
        }

        // POST: TabVersions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(TabVersionFormViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                // Need to return JSON failure to form
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            else
            {
                try
                {
                    if (viewModel.Id == 0)  // We are creating a new Tab
                    {
                        string currentUserId = User.GetUserId();

                        // Verify user has access to this tab
                        var tabInDb = _context.Tabs.SingleOrDefault(t => t.Id == viewModel.TabId && t.UserId == currentUserId);

                        // If we are not the owner, are we a contributor?
                        if (tabInDb == null)
                        {
                            tabInDb = (from tab in _context.Tabs
                                       join album in _context.Albums on tab.AlbumId equals album.Id
                                       join project in _context.Projects on album.ProjectId equals project.Id
                                       join contributor in _context.ProjectContributors on project.Id equals contributor.ProjectId
                                       where contributor.UserId == currentUserId && tab.Id == viewModel.TabId
                                       select tab).FirstOrDefault();

                            if (tabInDb == null)
                            {
                                return NotFound();
                            }
                        }

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

                            tabFile.Name = viewModel.FileData.FileName;
                            tabFile.DateCreated = DateTime.Now;
                        }

                        TabVersion tabVersion = new TabVersion()
                        {
                            // Create new TabVersion
                            UserId = User.GetUserId(),
                            Version = tabInDb.CurrentVersion + 1,        // Maybe do MAX(Version) + 1
                            Description = viewModel.Description,
                            DateCreated = DateTime.Now,
                            Tab = tabInDb,
                            TabFile = tabFile
                        };

                        _context.TabFiles.Add(tabFile);
                        _context.TabVersions.Add(tabVersion);

                        // Update Tab's current version to most recent TabVersion
                        tabInDb.CurrentVersion = tabVersion.Version;

                        _context.SaveChanges();

                        // Return tab name and id
                        return Json(new { name = tabInDb.Name, id = tabInDb.Id });
                    }
                    else
                    {
                        // TO DO: Return correct table when editing a tab version
                        return Json(new { });
                    }
                }
                catch
                {
                    // Need to return JSON failure to form
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }
            }
        }

        //// GET: TabVersions
        //[HttpGet]
        //public ActionResult Index(int id)
        //{
        //    // Return a list of all TabVersions belonging to the current user for current Tab (id)
        //    string currentUserId = User.GetUserId();

        //    try
        //    {
        //        var viewModel = new TabVersionIndexViewModel()
        //        {
        //            TabVersions = _context.TabVersions.Where(v => v.UserId == currentUserId && v.TabId == id).ToList(),
        //            TabName = _context.Tabs.Single(t => t.Id == id && t.UserId == currentUserId).Name,
        //            TabId = id,
        //            AlbumId = _context.Tabs.Single(t => t.Id == id && t.UserId == currentUserId).AlbumId
        //        };

        //        return View(viewModel);
        //    }
        //    catch
        //    {
        //        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        //    }
        //}

        // GET: TabVersions
        [HttpGet]
        public ActionResult UpdateTabVersionsTable(int id)
        {
            // Return a list of all TabVersions belonging to the current user for current Tab (id)
            string currentUserId = User.GetUserId();

            try
            {
                var tabInDb = _context.Tabs.SingleOrDefault(t => t.Id == id && t.UserId == currentUserId);
                
                // If we are not the owner, are we a contributor?
                if (tabInDb == null)
                {
                    tabInDb = (from tab in _context.Tabs
                               join album in _context.Albums on tab.AlbumId equals album.Id
                               join project in _context.Projects on album.ProjectId equals project.Id
                               join contributor in _context.ProjectContributors on project.Id equals contributor.ProjectId
                               where contributor.UserId == currentUserId && tab.Id == id
                               select tab).FirstOrDefault();

                    if (tabInDb == null)
                    {
                        return NotFound();
                    }
                }

                var tabVersionsInDb = _context.TabVersions
                        .Include(v => v.TabFile)
                        .Include(v => v.User)
                        .Where(v => v.TabId == id).ToList();

                var albumInDb = _context.Tabs.Include(t => t.Album).SingleOrDefault(t => t.Id == id).Album;

                if (tabVersionsInDb == null || tabInDb == null || albumInDb == null)
                {
                    return NotFound();
                }

                // Load Tab Versions into view model to be able to send over whether the current user is the owner of the tab
                List<TabVersionViewModel> tabVersions = new List<TabVersionViewModel>();

                foreach (var tabVersion in tabVersionsInDb)
                {
                    var tabVersionViewModel = new TabVersionViewModel()
                    {
                        TabVersion = tabVersion,
                        IsOwner = tabInDb.UserId == currentUserId
                    };

                    tabVersions.Add(tabVersionViewModel);
                }

                var viewModel = new TabVersionIndexViewModel()
                {
                    TabVersions = tabVersions,
                    TabName = tabInDb.Name,
                    TabId = id,
                    AlbumId = albumInDb.Id                    
                };

                return PartialView("_TabVersionsTable", viewModel);
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet]
        public ActionResult GetEmptyTabVersionsTable(int? id)
        {
            if (id == null)
            {
                return PartialView("_EmptyTabVersionsTable", null);
            }
            try
            {
                string currentUserId = User.GetUserId();

                var viewModel = new ProjectFormViewModel()
                {
                    Name = _context.Projects.SingleOrDefault(p => p.UserId == currentUserId && p.Id == id).Name,
                    Id = id
                };

                return PartialView("_EmptyTabVersionsTable", viewModel);
            }
            catch
            {
                return NotFound();
            }
        }

        public ActionResult Delete(int id)
        {
            try
            {
                // Verify current user has access to this TabVersion (id)
                string currentUserId = User.GetUserId();

                var tabVersionInDb = _context.TabVersions.SingleOrDefault(v => v.Id == id && v.UserId == currentUserId);

                if (tabVersionInDb == null)
                    return NotFound();

                // Get Tab Id to return to view
                var tabId = _context.TabVersions.Single(v => v.Id == id && v.UserId == currentUserId).TabId;

                _context.TabVersions.Remove(tabVersionInDb);
                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false });
            }            
        }

        public ApplicationUser GetCurrentUser()
        {
            // Get current user to store as reference in Project table
            string currentUserId = User.GetUserId();

            return _context.Users.FirstOrDefault(u => u.Id == currentUserId);
        }

        // GET: Tab version form
        [HttpGet]
        public ActionResult GetTabVersionFormPartialView(int tabId)
        {
            string currentUserId = User.GetUserId();

            // Verify user has access to this tab
            var tabInDb = _context.Tabs.SingleOrDefault(t => t.Id == tabId && t.UserId == currentUserId);

            // If we are not the owner, are we a contributor?
            if (tabInDb == null)
            {
                tabInDb = (from tab in _context.Tabs
                           join album in _context.Albums on tab.AlbumId equals album.Id
                           join project in _context.Projects on album.ProjectId equals project.Id
                           join contributor in _context.ProjectContributors on project.Id equals contributor.ProjectId
                           where contributor.UserId == currentUserId && tab.Id == tabId
                           select tab).FirstOrDefault();

                if (tabInDb == null)
                {
                    return NotFound();
                }
            }

            var viewModel = new TabVersionFormViewModel()
            {
                TabId = tabId,
                TabName = tabInDb.Name
            };

            return PartialView("_TabVersionForm", viewModel);
        }
    }
}