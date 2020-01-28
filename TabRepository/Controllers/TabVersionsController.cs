using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TabRepository.Data;
using TabRepository.Helpers;
using TabRepository.Models;
using TabRepository.ViewModels;

namespace TabRepository.Controllers
{
    [Authorize]
    public class TabVersionsController : Controller
    {
        private ApplicationDbContext _context;
        private UserAuthenticator _userAuthenticator;

        public TabVersionsController(ApplicationDbContext context, UserAuthenticator userAuthenticator)
        {
            _context = context;
            _userAuthenticator = userAuthenticator;
        }

        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
        }

        public ActionResult New(int id)
        {
            string currentUserId = User.GetUserId();

            // Verify current user has access to this Tab
            var tabInDb = (Tab)_userAuthenticator.CheckUserCreateAccess(Item.Tab, id, currentUserId);

            if (tabInDb == null)
                return Json(new { error = "Tab not found" });

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
                            var tabInDb = (Tab)_userAuthenticator.CheckUserCreateAccess(Item.Tab, viewModel.TabId, currentUserId);

                            if (tabInDb == null)
                                return Json(new { error = "Tab not found" });

                            // If we own this tab we need to make sure we have an active subscription or less than 50 tabs
                            var customerInDb = _context.StripeCustomers.Where(c => c.UserId == currentUserId).FirstOrDefault();

                            var tabVersionCount = 0;

                            if (tabInDb.User.AccountType == Models.AccountViewModels.AccountType.Free)
                            {
                                // Get a count of total tab versions that this user owns (i.e. their projects)
                                tabVersionCount = _context.TabVersions
                                    .Include(u => u.User)
                                    .Include(v => v.Tab)
                                    .Include(v => v.Tab.Album)
                                    .Include(v => v.Tab.Album.Project)
                                    .Where(v => v.Tab.Album.Project.UserId == tabInDb.UserId)
                                    .Count();

                                if (tabVersionCount >= 30)
                                {
                                    string error;

                                    if (tabInDb.UserId == currentUserId)
                                    {
                                        error = "<br /><br />You have met the 30 allowed free tab versions that are included with the free TabCollab account. You can continue to contribute to the projects of other musicians and view/edit your existing tabs.<br /><br />To upgrade your account to have UNLIMITED tab versions, go the the Account page.";
                                    }
                                    else
                                    {
                                        error = "<br /><br />The owner has met the 30 allowed free tab versions that are included with the free TabCollab account.";
                                    }

                                    return Json(new { error = error });
                                }
                            }

                            TabFile tabFile = new TabFile();

                            if (viewModel.FileData.Length > 0)
                            {
                                // Limit file size to 1 MB
                                if (viewModel.FileData.Length > 1000000)
                                {
                                    return Json(new { error = "File size limit is 1 MB" });
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

                            NotificationsController.AddNotification(
                                _context, 
                                NotificationType.TabVersionAdded, 
                                null, 
                                tabVersion.Tab.Album.ProjectId, 
                                userInDb, 
                                tabVersion.Version.ToString(), 
                                tabVersion.Tab.Name
                            );

                            transaction.Commit();

                            // Return tab name and id
                            return Json(new { name = tabInDb.Name, id = tabInDb.Id });
                        }
                    }
                    else
                    {
                        var tabVersionInDb = (TabVersion)_userAuthenticator.CheckUserEditAccess(Item.TabVersion, viewModel.Id, currentUserId);

                        // If we are not the owner, are we the tab owner?
                        if (tabVersionInDb == null)
                        {
                        return Json(new { error = "Tab version not found" });
                        }

                        tabVersionInDb.Description = viewModel.Description;

                        _context.TabVersions.Update(tabVersionInDb);
                        _context.SaveChanges();

                        return Json(new { name = tabVersionInDb.Tab.Name, id = tabVersionInDb.Tab.Id });
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

        // GET: TabVersions
        [HttpGet]
        public ActionResult UpdateTabVersionsTable(int id)
        {
            // Return a list of all TabVersions belonging to the current user for current Tab (id)
            string currentUserId = User.GetUserId();

            try
            {
                var tabInDb = (Tab)_userAuthenticator.CheckUserReadAccess(Item.Tab, id, currentUserId);
                
                // If we are not the owner, are we a contributor?
                if (tabInDb == null)
                {                    
                    return RedirectToAction("GetEmptyTabVersionsTable", "TabVersions");
                }

                // Load Tab Versions into view model to be able to send over whether the current user is the owner of the tab
                List<TabVersionViewModel> tabVersions = new List<TabVersionViewModel>();

                foreach (var tabVersion in tabInDb.TabVersions)
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
                    AlbumId = tabInDb.Album.Id                    
                };

                return PartialView("_TabVersionsTable", viewModel);
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }

        [HttpGet]
        public ActionResult GetTabVersionsList(int id)
        {
            // Return a list of all TabVersions belonging to the current user for current Tab (id)
            string currentUserId = User.GetUserId();

            try
            {
                var tabInDb = (Tab)_userAuthenticator.CheckUserReadAccess(Item.Tab, id, currentUserId);

                // If we are not the owner, are we a contributor?
                if (tabInDb == null)
                {
                    return Json(new { error = "Tab not found" });
                }

                // Load Tab Versions into view model to be able to send over whether the current user is the owner of the tab
                List<TabVersionViewModel> tabVersions = new List<TabVersionViewModel>();

                foreach (var tabVersion in tabInDb.TabVersions)
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
                    AlbumId = tabInDb.Album.Id
                };

                return PartialView("_TabVersionList", viewModel);
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
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
                return PartialView("_EmptyTabVersionsTable", null);
            }
        }

        [HttpDelete]
        public ActionResult Delete(int id)
        {
            try
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    // Verify current user has access to this TabVersion (id)
                    string currentUserId = User.GetUserId();
                    string currentUsername = User.GetUsername();
                    var userInDb = _context.Users.Where(u => u.Id == currentUserId).FirstOrDefault();

                    if (userInDb == null)
                    {
                        return Json(new { error = "User not found" });
                    }

                    // Are we the project owner?
                    var tabVersionInDb = (TabVersion)_userAuthenticator.CheckUserDeleteAccess(Item.TabVersion, id, currentUserId);

                    if (tabVersionInDb == null)
                    {
                        return Json(new { error = "Tab version not found" });
                    }

                    _context.TabVersions.Remove(tabVersionInDb);
                    _context.SaveChanges();

                    NotificationsController.AddNotification(
                        _context, 
                        NotificationType.TabVersionDeleted, 
                        null, 
                        tabVersionInDb.Tab.Album.ProjectId, 
                        userInDb, 
                        tabVersionInDb.Version.ToString(), 
                        tabVersionInDb.Tab.Name
                    );

                    transaction.Commit();

                    return Json(new { success = true });
                }
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
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
                    var tabInDb = (Tab)_userAuthenticator.CheckUserCreateAccess(Item.Tab, tabId, currentUserId);

                    // If we are not the owner, are we a contributor?
                    if (tabInDb == null)
                    {
                        return Json(new { error = "Tab not found" });
                    }

                    viewModel.TabId = tabId;
                    viewModel.TabName = tabInDb.Name;
                }
                else
                {
                    var tabVersionInDb = (TabVersion)_userAuthenticator.CheckUserEditAccess(Item.TabVersion, tabVersionId, currentUserId);

                    if (tabVersionInDb == null)
                    {
                        return Json(new { error = "Tab version not found" });
                    }

                    viewModel.Id = tabVersionInDb.Id;
                    viewModel.TabId = tabId;
                    viewModel.Description = tabVersionInDb.Description;
                }

                return PartialView("_TabVersionForm", viewModel);
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
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
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }
    }
}