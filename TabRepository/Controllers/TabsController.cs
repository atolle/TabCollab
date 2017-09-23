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
            var albumInDb = _context.Albums.Single(a => a.Id == id && a.UserId == currentUserId);
            if (albumInDb == null)
                return NotFound();

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
            if (!ModelState.IsValid)
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            try
            {
                string currentUserId = User.GetUserId();

                if (viewModel.Id == 0)  // We are creating a new Tab
                {
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

                    // Create new Tab
                    Tab tab = new Tab()
                    {
                        UserId = User.GetUserId(),
                        Album = _context.Albums.Single(p => p.Id == viewModel.AlbumId && p.UserId == currentUserId),
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
                        Tab = tab,
                        TabFile = tabFile
                    };

                    tabVersion.TabFile = tabFile;

                    _context.Tabs.Add(tab);
                    _context.TabVersions.Add(tabVersion);
                    _context.TabFiles.Add(tabFile);
                    _context.SaveChanges();

                    // Return tab name and id
                    return Json(new { name = tab.Name, id = tab.Id });
                }
                else
                {
                    var tabInDb = _context.Tabs.SingleOrDefault(p => p.Id == viewModel.Id && p.UserId == currentUserId);

                    // If current user does not have access to project or project does not exist
                    if (tabInDb == null)
                    {
                        return NotFound();
                    }

                    tabInDb.Name = viewModel.Name;
                    tabInDb.Description = viewModel.Description;
                    tabInDb.DateModified = DateTime.Now;

                    _context.Tabs.Update(tabInDb);
                    _context.SaveChanges();

                    return Json(new { name = tabInDb.Name, id = tabInDb.Id });
                }
            }
            catch
            {
                // Need to return failure to form
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }                       
        }

        // GET: Tabs
        public ActionResult Index(int id)
        {
            // Return a list of all Tabs belonging to the current user for current Project (id)
            string currentUserId = User.GetUserId();

            var viewModel = new TabIndexViewModel();

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
            int projectId = _context.Tabs.Single(t => t.Id == id && t.UserId == currentUserId).AlbumId;

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
                    var albumInDb = _context.Albums.Single(a => a.Id == albumId && a.UserId == currentUserId);
                    if (albumInDb == null)
                    {
                        return NotFound();
                    }

                    viewModel.AlbumId = albumInDb.Id;
                    viewModel.AlbumName = albumInDb.Name;
                }
                else
                {
                    var tabInDb = _context.Tabs.Single(p => p.Id == tabId && p.UserId == currentUserId);
                    if (tabInDb == null)
                    {
                        return NotFound();
                    }

                    viewModel.Id = tabInDb.Id;
                    viewModel.Name = tabInDb.Name;
                    viewModel.Description = tabInDb.Description;
                }

                return PartialView("_TabForm", viewModel);
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet]
        public ActionResult GetTabListPartialView(int albumId)
        {
            string currentUserId = User.GetUserId();
            List<TabIndexViewModel> viewModel = new List<TabIndexViewModel>();

            if (albumId == 0)
            {
                try
                {
                    // Return a list of all Projects belonging to the current user
                    var tabs = _context.Tabs.Include(u => u.User)
                        .Include(a => a.Album)
                        .Where(a => a.UserId == currentUserId)
                        .OrderBy(a => a.Name)
                        .ToList();

                    foreach (var tab in tabs)
                    {
                        var elem = new TabIndexViewModel()
                        {
                            Id = tab.Id,
                            UserId = tab.UserId,
                            Name = tab.Name,
                            AlbumId = tab.Album.Id,
                            AlbumName = tab.Album.Name,
                            DateCreated = tab.DateCreated,
                            DateModified = tab.DateModified,
                            User = tab.User,
                            TabVersions = tab.TabVersions
                        };

                        // Add projects to project view model
                        viewModel.Add(elem);
                    }

                    return PartialView("_TabList", viewModel);
                }
                catch
                {
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }

            }
            else
            {
                try
                {
                    // Return a list of all Projects belonging to the current user
                    var tabs = _context.Tabs.Include(u => u.User)
                        .Include(a => a.Album)
                        .Where(a => a.UserId == currentUserId && a.AlbumId == albumId)
                        .OrderBy(a => a.Name)
                        .ToList();

                    foreach (var tab in tabs)
                    {
                        var elem = new TabIndexViewModel()
                        {
                            Id = tab.Id,
                            UserId = tab.UserId,
                            Name = tab.Name,
                            AlbumId = tab.Album.Id,
                            AlbumName = tab.Album.Name,
                            DateCreated = tab.DateCreated,
                            DateModified = tab.DateModified,
                            User = tab.User,
                            TabVersions = tab.TabVersions
                        };

                        // Add projects to project view model
                        viewModel.Add(elem);
                    }

                    return PartialView("_TabList", viewModel);
                }
                catch
                {
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }

            }
        }
    }
}