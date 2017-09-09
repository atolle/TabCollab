using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
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
                return View("TabVersionForm", viewModel);
            }

            if (viewModel.Id == 0)  // We are creating a new Tab
            {
                string currentUserId = User.GetUserId();

                int currentVersion = _context.Tabs.Single(t => t.Id == viewModel.TabId && t.UserId == currentUserId).CurrentVersion;

                var tab = _context.Tabs.Where(t => t.Id == viewModel.TabId && t.UserId == currentUserId).Single();

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
                    Version = currentVersion + 1,        // Maybe do MAX(Version) + 1
                    Description = viewModel.Description,
                    DateCreated = DateTime.Now,
                    Tab = tab,
                    TabFile = tabFile
                };

                _context.TabFiles.Add(tabFile);
                _context.TabVersions.Add(tabVersion);

                // Update Tab's current version to most recent TabVersion
                tab.CurrentVersion = tabVersion.Version;
            }

            _context.SaveChanges();

            // Redirect to list of tab versions for current tab
            //return RedirectToAction("Index", "TabVersions", new { id = viewModel.TabId }); 
            return RedirectToAction("Main", "Projects"); 
        }

        // POST: TabVersions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxSave(TabVersionFormViewModel viewModel)
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

                        int currentVersion = _context.Tabs.Single(t => t.Id == viewModel.TabId && t.UserId == currentUserId).CurrentVersion;

                        var tab = _context.Tabs.Where(t => t.Id == viewModel.TabId && t.UserId == currentUserId).Single();

                        //byte[] tabData = new byte[viewModel.FileData.Length];
                        //viewModel.FileData.InputStream.Read(tabData, 0, tabData.Length);

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
                            Version = currentVersion + 1,        // Maybe do MAX(Version) + 1
                            Description = viewModel.Description,
                            DateCreated = DateTime.Now,
                            Tab = tab,
                            TabFile = tabFile
                        };

                        _context.TabFiles.Add(tabFile);
                        _context.TabVersions.Add(tabVersion);

                        // Update Tab's current version to most recent TabVersion
                        tab.CurrentVersion = tabVersion.Version;

                        _context.SaveChanges();

                        // Return tab name and id
                        return Json(new { name = tab.Name, id = tab.Id });
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

        // GET: TabVersions
        [HttpGet]
        public ActionResult Index(int id)
        {
            // Return a list of all TabVersions belonging to the current user for current Tab (id)
            string currentUserId = User.GetUserId();

            var viewModel = new TabVersionIndexViewModel()
            {
                TabVersions = _context.TabVersions.Where(v => v.UserId == currentUserId && v.TabId == id).ToList(),
                TabName = _context.Tabs.Single(t => t.Id == id && t.UserId == currentUserId).Name,
                TabId = id,
                ProjectId = _context.Tabs.Single(t => t.Id == id && t.UserId == currentUserId).ProjectId
            };

            return View(viewModel);
        }

        // GET: TabVersions
        [HttpGet]
        public ActionResult UpdateTabVersionsTable(int id)
        {
            // Return a list of all TabVersions belonging to the current user for current Tab (id)
            string currentUserId = User.GetUserId();

            var viewModel = new TabVersionIndexViewModel()
            {
                TabVersions = _context.TabVersions
                    .Include(v => v.TabFile)
                    .Include(v => v.User)
                    .Where(v => v.UserId == currentUserId && v.TabId == id).ToList(),
                TabName = _context.Tabs.Single(t => t.Id == id && t.UserId == currentUserId).Name,
                TabId = id,
                ProjectId = _context.Tabs.Single(t => t.Id == id && t.UserId == currentUserId).ProjectId
            };

            return PartialView("_TabVersionsTable", viewModel);
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
            // Verify current user has access to this TabVersion (id)
            string currentUserId = User.GetUserId();

            var tabVersionInDb = _context.TabVersions.SingleOrDefault(v => v.Id == id && v.UserId == currentUserId);
         
            if (tabVersionInDb == null)
                return NotFound();

            // Get Tab Id to return to view
            var tabId = _context.TabVersions.Single(v => v.Id == id && v.UserId == currentUserId).TabId;

            _context.TabVersions.Remove(tabVersionInDb);
            _context.SaveChanges();
            
            //return RedirectToAction("Index", "TabVersions", new { id = tabId }); 
            return RedirectToAction("Main", "Projects"); 
        }

        public ActionResult AjaxDelete(int id)
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
        public ActionResult GetTabVersionFormPartialView(int id)
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

            return PartialView("_TabVersionForm", viewModel);
        }
    }
}