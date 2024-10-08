﻿using Microsoft.AspNetCore.Authorization;
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
using TabRepository.Models.AccountViewModels;
using TabRepository.ViewModels;

namespace TabRepository.Controllers
{
    [Authorize]
    public class TabsController : Controller
    {
        private ApplicationDbContext _context;
        private UserAuthenticator _userAuthenticator;

        public TabsController(ApplicationDbContext context, UserAuthenticator userAuthenticator)
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

            // Verify current user has access to this project
            var albumInDb = (Album)_userAuthenticator.CheckUserCreateAccess(Item.Album, id, currentUserId);
            if (albumInDb == null)
                return Json(new { error = "Album not found" });

            var viewModel = new TabFormViewModel()
            {
                AlbumId = albumInDb.Id,
                AlbumName = albumInDb.Name
            };

            return View("TabForm", viewModel);
        }

        // POST: Tabs
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(TabFormViewModel viewModel)
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

                    if (viewModel.Id == 0)  // We are creating a new Tab
                    {
                        using (var transaction = _context.Database.BeginTransaction())
                        {
                            // Verify current user has access to this project
                            var albumInDb = (Album)_userAuthenticator.CheckUserCreateAccess(Item.Album, viewModel.AlbumId, currentUserId);

                            if (albumInDb == null)
                                return Json(new { error = "Album not found" });

                            int order = 0;

                            // Order is max order + 1
                            if (albumInDb.Tabs != null && albumInDb.Tabs.Count > 0)
                            {
                                order = Convert.ToInt32(albumInDb.Tabs.Max(t => t.Order)) + 1;
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

                            // Create new Tab
                            Tab tab = new Tab()
                            {
                                UserId = albumInDb.UserId,
                                Album = albumInDb,
                                Name = viewModel.Name,
                                Description = viewModel.Description,
                                DateCreated = DateTime.Now,
                                DateModified = DateTime.Now,
                                CurrentVersion = 1,
                                Order = order
                            };

                            // Create first Tab Version
                            TabVersion tabVersion = new TabVersion()
                            {
                                Version = 1,
                                Description = viewModel.Description,
                                UserId = currentUserId,
                                DateCreated = tab.DateCreated,
                                Tab = tab,
                                TabFile = tabFile
                            };

                            tabVersion.TabFile = tabFile;

                            _context.Tabs.Add(tab);
                            _context.TabVersions.Add(tabVersion);
                            _context.TabFiles.Add(tabFile);
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
                                NotificationType.TabAdded, 
                                null, 
                                tab.Album.ProjectId, 
                                userInDb, 
                                tab.Name, 
                                tab.Album.Name
                            );

                            transaction.Commit();

                            // Return tab name and id
                            return Json(new { name = tab.Name, id = tab.Id });
                        }
                    }
                    else
                    {
                        var tabInDb = (Tab)_userAuthenticator.CheckUserEditAccess(Item.Tab, viewModel.Id, currentUserId);

                        // If current user does not have access to project or project does not exist
                        if (tabInDb == null)
                        {
                            return Json(new { error = "Tab not found" });
                        }

                        tabInDb.Name = viewModel.Name;
                        tabInDb.Description = viewModel.Description;
                        tabInDb.DateModified = DateTime.Now;

                        _context.Tabs.Update(tabInDb);
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

        [AllowAnonymous]
        public ActionResult Demo()
        {
            return View();
        }

        public ActionResult Index()
        {
            string currentUserId = User.GetUserId();

            TabIndexViewModel viewModel = new TabIndexViewModel();
            viewModel.ProjectIndexViewModels = new List<ProjectIndexViewModel>();

            try
            {
                // Find projects for which user is owner
                var projects = _userAuthenticator.GetAllItems(Item.Project, null, currentUserId).Cast<Project>().OrderBy(p => p.Name).ToList();

                var userInDb = _context.Users
                    .Where(u => u.Id == currentUserId)
                    .FirstOrDefault();

                bool allowNewTabs = true;
                bool allowNewTabVersions = true;

                viewModel.TabTutorialShown = userInDb.TabTutorialShown;
                viewModel.TabTutorialMobileShown = userInDb.TabTutorialMobileShown;
                viewModel.AllowNewTabs = allowNewTabs;
                viewModel.AllowNewTabVersions = allowNewTabVersions;

                foreach (var project in projects)
                {
                    project.Albums = project.Albums.OrderBy(a => a.Order).ToList();
                    ProjectIndexViewModel vm = new ProjectIndexViewModel()
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
                    foreach (var album in vm.Albums)
                    {
                        album.Tabs = album.Tabs.OrderBy(t => t.Order).ToList();
                    }

                    // Add projects to project view model
                    viewModel.ProjectIndexViewModels.Add(vm);
                }

                return View(viewModel);
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
                    // Verify user has access to current Tab (id)
                    string currentUserId = User.GetUserId();
                    string currentUsername = User.GetUsername();
                    var userInDb = _context.Users.Where(u => u.Id == currentUserId).FirstOrDefault();

                    if (userInDb == null)
                    {
                        return Json(new { error = "User not found" });
                    }

                    var tabInDb = (Tab)_userAuthenticator.CheckUserDeleteAccess(Item.Tab, id, currentUserId);

                    if (tabInDb == null)
                    {
                        return Json(new { error = "Tab not found" });
                    }

                    _context.Tabs.Remove(tabInDb);
                    _context.SaveChanges();

                    NotificationsController.AddNotification(
                        _context, 
                        NotificationType.TabDeleted, 
                        null, 
                        tabInDb.Album.ProjectId, 
                        userInDb, 
                        tabInDb.Name, 
                        tabInDb.Album.Name
                    );

                    transaction.Commit();
                }

                return Json(new { success = true });
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

        // GET: Tab form
        // Two scenarios for this method:
        // albumId == 0 && tabId != 0 -> editing tab
        // albumId != 0 && tabId == 0 -> creating new tab
        [HttpGet]
        public ActionResult GetTabFormPartialView(int albumId, int tabId)
        {
            try
            {
                string currentUserId = User.GetUserId();
                var viewModel = new TabFormViewModel();

                if (tabId == 0)
                {
                    // Verify current user has access to this album
                    var albumInDb = (Album)_userAuthenticator.CheckUserCreateAccess(Item.Album, albumId, currentUserId);

                    // If there is no album matching this album Id and this user Id, check to see if this user is a contributor
                    if (albumInDb == null)
                    {
                        return Json(new { error = "Album not found" });
                    }

                    viewModel.AlbumId = albumInDb.Id;
                    viewModel.AlbumName = albumInDb.Name;
                }
                else
                {
                    var tabInDb = (Tab)_userAuthenticator.CheckUserEditAccess(Item.Tab, tabId, currentUserId);

                    if (tabInDb == null)
                    {
                        return Json(new { error = "Tab not found" });
                    }

                    viewModel.Id = tabInDb.Id;
                    viewModel.Name = tabInDb.Name;
                    viewModel.Description = tabInDb.Description;
                }

                return PartialView("_TabForm", viewModel);
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }

        [HttpGet]
        public ActionResult GetTabSelectionPartialView(int albumId)
        {
            try
            {
                string currentUserId = User.GetUserId();
                TabSelectionViewModel viewModel = new TabSelectionViewModel();

                viewModel.Tabs = new List<TabViewModel>();
                viewModel.AlbumId = albumId;

                var tabs = _userAuthenticator.GetAllItems(Item.Tab, albumId, currentUserId).Cast<Tab>().ToList();

                foreach (var tab in tabs)
                {
                    var elem = new TabViewModel()
                    {
                        Id = tab.Id,
                        Name = tab.Name
                    };

                    // Add projects to project view model
                    viewModel.Tabs.Add(elem);
                }

                return PartialView("_TabSelection", viewModel);
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }

        [HttpGet]
        public ActionResult GetTabListPartialView()
        {
            string currentUserId = User.GetUserId();

            TabIndexViewModel viewModel = new TabIndexViewModel();
            viewModel.ProjectIndexViewModels = new List<ProjectIndexViewModel>();

            try
            {
                // Find projects for which user is owner
                var projects = _userAuthenticator.GetAllItems(Item.Project, null, currentUserId).Cast<Project>().ToList();

                var userInDb = _context.Users
                    .Where(u => u.Id == currentUserId)
                    .FirstOrDefault();

                foreach (var project in projects)
                {
                    project.Albums = project.Albums.OrderBy(a => a.Order).ToList();
                    ProjectIndexViewModel vm = new ProjectIndexViewModel()
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
                    foreach (var album in vm.Albums)
                    {
                        album.Tabs = album.Tabs.OrderBy(t => t.Order).ToList();
                    }

                    // Add projects to project view model
                    viewModel.ProjectIndexViewModels.Add(vm);
                }

                return PartialView("_TabList", viewModel); ;
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }

        [HttpPost]
        public ActionResult ReorderTabs(List<int> tabIds)
        {
            string currentUserId = User.GetUserId();

            try
            {
                for (int i = 0; i < tabIds.Count; i++)
                {
                    var tabInDb = (Tab)_userAuthenticator.CheckUserEditAccess(Item.Tab, tabIds[i], currentUserId);

                    if (tabInDb.UserId != currentUserId)
                    {
                        Json(new { error = "Tab not found" });
                    }

                    tabInDb.Order = i;
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
        public ActionResult MoveTab(int tabId, int albumId)
        {
            string currentUserId = User.GetUserId();

            try
            {
                var albumInDb = (Album)_userAuthenticator.CheckUserEditAccess(Item.Album, albumId, currentUserId);

                if (albumInDb == null)
                {
                    return Json(new { error = "Album not found" });
                }

                var tabInDb = (Tab)_userAuthenticator.CheckUserEditAccess(Item.Tab, tabId, currentUserId);

                if (tabInDb == null)
                {
                    return Json(new { error = "Tab not found" });
                }

                tabInDb.Album = albumInDb;

                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }

        [HttpPost]
        public ActionResult TabTutorialShown()
        {
            string currentUserId = User.GetUserId();

            try
            {
                var userInDb = _context.Users.Where(u => u.Id == currentUserId).FirstOrDefault();

                if (userInDb == null)
                {
                    return Json(new { error = "User not found" });
                }

                userInDb.TabTutorialShown = true;

                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }

        [HttpPost]
        public ActionResult TabTutorialMobileShown()
        {
            string currentUserId = User.GetUserId();

            try
            {
                var userInDb = _context.Users.Where(u => u.Id == currentUserId).FirstOrDefault();

                if (userInDb == null)
                {
                    return Json(new { error = "User not found" });
                }

                userInDb.TabTutorialMobileShown = true;

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