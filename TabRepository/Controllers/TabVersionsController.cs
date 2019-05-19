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
            try
            {
                if (ModelState.IsValid)
                {
                    string currentUserId = User.GetUserId();
                    string currentUsername = User.GetUsername();
                    var userInDb = _context.Users.Where(u => u.Id == currentUserId).FirstOrDefault();

                    if (viewModel.Id == 0)  // We are creating a new Tab
                    {
                        using (var transaction = _context.Database.BeginTransaction())
                        {
                            // Verify user has access to this tab
                            var tabInDb = _context.Tabs.Include(t => t.Album).ThenInclude(a => a.Project).SingleOrDefault(t => t.Id == viewModel.TabId && t.UserId == currentUserId);

                            // If we own this tab we need to make sure we have an active subscription or less than 50 tabs
                            if (tabInDb != null)
                            {
                                var subscriptionExpiration = _context.Users
                                    .Where(u => u.Id == currentUserId)
                                    .Select(u => u.SubscriptionExpiration)
                                    .FirstOrDefault();

                                var tabVersionCount = 0;

                                if (userInDb.AccountType == Models.AccountViewModels.AccountType.Free || (userInDb.AccountType == Models.AccountViewModels.AccountType.Subscription && (int)(subscriptionExpiration - DateTime.Now).Value.TotalDays < 0))
                                {
                                    // Get a count of total tab versions that this user owns (i.e. their projects)
                                    tabVersionCount = _context.TabVersions.Include(u => u.User)
                                        .Include(v => v.Tab)
                                        .Include(v => v.Tab.Album)
                                        .Include(v => v.Tab.Album.Project)
                                        .Where(v => v.Tab.Album.Project.UserId == currentUserId)
                                        .Count();

                                    if (tabVersionCount >= 50)
                                    {
                                        if (subscriptionExpiration == null)
                                        {
                                            return StatusCode(StatusCodes.Status500InternalServerError, "<br /><br />You have met the 50 allowed free tab versions that are included with the free TabCollab account. You can continue to contribute to the projects of other musicians and view/edit your existing tabs.<br /><br />To upgrade your account to have UNLIMITED tab versions, go the the Account page.");
                                        }
                                        else
                                        {
                                            return StatusCode(StatusCodes.Status500InternalServerError, "<br /><br />Your TabCollab subscription has expired. You can continue to contribute to the projects of other musicians and view/edit your existing tabs.<br /><br />To renew your subscription, go the the Account page.");
                                        }
                                    }
                                }
                            }

                            // If we are not the owner, are we a contributor?
                            if (tabInDb == null)
                            {
                                tabInDb = (from tab in _context.Tabs
                                           join album in _context.Albums on tab.AlbumId equals album.Id
                                           join project in _context.Projects on album.ProjectId equals project.Id
                                           join contributor in _context.ProjectContributors on project.Id equals contributor.ProjectId
                                           where contributor.UserId == currentUserId && tab.Id == viewModel.TabId
                                           select tab)
                                           .Include(t => t.Album)
                                           .ThenInclude(a => a.Project)
                                           .FirstOrDefault();

                                if (tabInDb == null)
                                {
                                    return NotFound();
                                }
                                else
                                {
                                    // Make sure the owner's account is not expired 
                                    string otherUserId = tabInDb.UserId;
                                    var otherUserInDb = _context.Users.Where(u => u.Id == otherUserId).FirstOrDefault();

                                    var subscriptionExpiration = _context.Users
                                        .Where(u => u.Id == otherUserId)
                                        .Select(u => u.SubscriptionExpiration)
                                        .FirstOrDefault();

                                    var tabVersionCount = 0;

                                    if (otherUserInDb.AccountType == Models.AccountViewModels.AccountType.Free || (otherUserInDb.AccountType == Models.AccountViewModels.AccountType.Subscription &&  (int)(subscriptionExpiration - DateTime.Now).Value.TotalDays < 0))
                                    {
                                        // Get a count of total tab versions that this user owns (i.e. their projects)
                                        tabVersionCount = _context.TabVersions.Include(u => u.User)
                                            .Include(v => v.Tab)
                                            .Include(v => v.Tab.Album)
                                            .Include(v => v.Tab.Album.Project)
                                            .Where(v => v.Tab.Album.Project.UserId == otherUserId)
                                            .Count();

                                        if (tabVersionCount >= 50)
                                        {
                                            if (subscriptionExpiration == null)
                                            {
                                                return StatusCode(StatusCodes.Status500InternalServerError, "<br /><br />The owner has met the 50 allowed free tab versions that are included with the free TabCollab account.");
                                            }
                                            else
                                            {
                                                return StatusCode(StatusCodes.Status500InternalServerError, "<br /><br />The owner's TabCollab subscription has expired.");
                                            }
                                        }
                                    }
                                }
                            }

                            TabFile tabFile = new TabFile();

                            if (viewModel.FileData.Length > 0)
                            {
                                // Limit file size to 1 MB
                                if (viewModel.FileData.Length > 1000000)
                                {
                                    return StatusCode(StatusCodes.Status500InternalServerError, "File size limit is 1 MB");
                                }
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

                            var userTabVersionInDb = _context
                                .UserTabVersions
                                .Where(v => v.UserId == currentUserId && v.TabId == tabVersion.TabId)
                                .FirstOrDefault();

                            if (userTabVersionInDb != null)
                            {
                                if (tabVersion.Version > userTabVersionInDb.Version)
                                {
                                    userTabVersionInDb.Version = tabVersion.Version;
                                    _context.UserTabVersions.Update(userTabVersionInDb);
                                    _context.SaveChanges();
                                }
                            }
                            else
                            {
                                UserTabVersion userTabVersion = new UserTabVersion
                                {
                                    UserId = currentUserId,
                                    TabId = tabVersion.TabId,
                                    Version = tabVersion.Version
                                };

                                _context.UserTabVersions.Add(userTabVersion);
                                _context.SaveChanges();
                            }

                            NotificationsController.AddNotification(_context, NotificationType.TabVersionAdded, null, tabVersion.Tab.Album.ProjectId, currentUsername, currentUserId, tabVersion.Version.ToString(), tabVersion.Tab.Name);

                            transaction.Commit();

                            // Return tab name and id
                            return Json(new { name = tabInDb.Name, id = tabInDb.Id });
                        }
                    }
                    else
                    {
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

                        var tabVersionInDb = _context.TabVersions.SingleOrDefault(v => v.Id == viewModel.Id && v.UserId == currentUserId);

                        // If we are not the owner, are we a contributor?
                        if (tabVersionInDb == null)
                        {
                            tabVersionInDb = (from tabVersion in _context.TabVersions
                                              join tab in _context.Tabs on tabVersion.TabId equals tab.Id
                                              where tab.UserId == currentUserId && tabVersion.Id == viewModel.Id
                                              select tabVersion).Include(u => u.User).FirstOrDefault();

                            if (tabVersionInDb == null)
                            {
                                return NotFound();
                            }
                        }

                        tabVersionInDb.Description = viewModel.Description;

                        _context.TabVersions.Update(tabVersionInDb);
                        _context.SaveChanges();

                        return Json(new { name = tabInDb.Name, id = tabInDb.Id });
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
                        .Where(v => v.TabId == id)
                        .OrderBy(v => v.Version)
                        .ToList();

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
                        IsOwner = tabInDb.UserId == currentUserId || tabVersion.UserId == currentUserId
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
        public ActionResult GetTabVersionsList(int id)
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
                        .Where(v => v.TabId == id)
                        .OrderBy(v => v.Version)
                        .ToList();

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
                        IsOwner = tabInDb.UserId == currentUserId || tabVersion.UserId == currentUserId
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

                return PartialView("_TabVersionList", viewModel);
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
                using (var transaction = _context.Database.BeginTransaction())
                {
                    // Verify current user has access to this TabVersion (id)
                    string currentUserId = User.GetUserId();
                    string currentUsername = User.GetUsername();

                    var tabVersionInDb = _context
                        .TabVersions
                        .Include(v => v.Tab)
                        .ThenInclude(t => t.Album)
                        .ThenInclude(a => a.Project)
                        .SingleOrDefault(v => v.Id == id && v.UserId == currentUserId);

                    if (tabVersionInDb == null)
                    {
                        tabVersionInDb = (from tabVersion in _context.TabVersions
                                          join tab in _context.Tabs on tabVersion.TabId equals tab.Id
                                          where tab.UserId == currentUserId && tabVersion.Id == id
                                          select tabVersion)
                                          .Include(u => u.User)
                                          .Include(u => u.Tab)
                                          .ThenInclude(t => t.Album)
                                          .ThenInclude(a => a.Project)
                                          .FirstOrDefault();

                        if (tabVersionInDb == null)
                        {
                            return Json(new { success = false });
                        }
                    }

                    _context.TabVersions.Remove(tabVersionInDb);
                    _context.SaveChanges();

                    NotificationsController.AddNotification(_context, NotificationType.TabVersionDeleted, null, tabVersionInDb.Tab.Album.ProjectId, currentUsername, currentUserId, tabVersionInDb.Version.ToString(), tabVersionInDb.Tab.Name);

                    transaction.Commit();

                    return Json(new { success = true });
                }
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
        public ActionResult GetTabVersionFormPartialView(int tabId, int tabVersionId)
        {
            try
            { 
                string currentUserId = User.GetUserId();
                var viewModel = new TabVersionFormViewModel();

                if (tabVersionId == 0)
                {
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

                    viewModel.TabId = tabId;
                    viewModel.TabName = tabInDb.Name;
                }
                else
                {
                    var tabVersionInDb = _context.TabVersions.SingleOrDefault(v => v.Id == tabVersionId && v.UserId == currentUserId);

                    if (tabVersionInDb == null)
                    {
                        tabVersionInDb = (from tabVersion in _context.TabVersions
                                          join tab in _context.Tabs on tabVersion.TabId equals tab.Id
                                          where tab.UserId == currentUserId && tabVersion.Id == tabVersionId
                                          select tabVersion).Include(u => u.User).FirstOrDefault();

                        if (tabVersionInDb == null)
                        {
                            return Json(new { success = false });
                        }
                    }

                    viewModel.Id = tabVersionInDb.Id;
                    viewModel.TabId = tabId;
                    viewModel.Description = tabVersionInDb.Description;
                }

                return PartialView("_TabVersionForm", viewModel);
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet]
        public ActionResult CheckLatestTabVersion(int tabId)
        {
            bool hasLatest = false;

            try
            {
                string currentUserId = User.GetUserId();

                int? latestUserVersion = _context
                    .UserTabVersions
                    .Where(v => v.TabId == tabId && v.UserId == currentUserId)
                    .Select(v => v.Version)
                    .FirstOrDefault();

                int? latestVersion = _context
                    .TabVersions
                    .Where(v => v.TabId == tabId)                    
                    .Max(v => v.Version);

                if (latestUserVersion != null && latestVersion != null)
                {
                    if (latestUserVersion >= latestVersion)
                    {
                        hasLatest = true;
                    }
                }

                return Json(new { hasLatest });
            }
            catch
            {
                return Json(new { hasLatest });
            }
        }
    }
}