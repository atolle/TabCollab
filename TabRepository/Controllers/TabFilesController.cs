using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using TabRepository.Data;
using TabRepository.Helpers;
using TabRepository.Models;

namespace TabRepository.Controllers
{
    [Authorize]
    public class TabFilesController : Controller
    {
        private ApplicationDbContext _context;
        private UserAuthenticator _userAuthenticator;

        public TabFilesController(ApplicationDbContext context, UserAuthenticator userAuthenticator)
        {
            _context = context;
            _userAuthenticator = userAuthenticator;
        }

        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
        }

        // GET: TabFile
        public ActionResult Download(int id)
        {
            string currentUserId = User.GetUserId();

            try
            {
                // Do we own the project?
                var tabVersionInDb = (TabVersion)_userAuthenticator.CheckUserReadAccess(Item.TabVersion, id, currentUserId);

                // If we are not the owner, are we a contributor?
                if (tabVersionInDb == null)
                {
                    return Json(new { error = "Tab version not found" });
                }

                var tabFileInDb = _context.TabFiles.Single(f => f.Id == id);

                if (tabFileInDb == null)
                {
                    return Json(new { error = "Tab file not found" });
                }

                byte[] fileBytes = tabFileInDb.TabData;
                string fileName = tabFileInDb.Name;

                var userTabVersionInDb = _context
                    .UserTabVersions
                    .Where(v => v.UserId == currentUserId && v.TabId == tabVersionInDb.TabId)
                    .FirstOrDefault();

                if (userTabVersionInDb != null)
                {
                    if (tabVersionInDb.Version > userTabVersionInDb.Version)
                    {
                        userTabVersionInDb.Version = tabVersionInDb.Version;
                        _context.UserTabVersions.Update(userTabVersionInDb);
                        _context.SaveChanges();
                    }
                }
                else
                {
                    UserTabVersion userTabVersion = new UserTabVersion
                    {
                        UserId = currentUserId,
                        TabId = tabVersionInDb.TabId,
                        Version = tabVersionInDb.Version
                    };

                    _context.UserTabVersions.Add(userTabVersion);
                    _context.SaveChanges();
                }

                return File(fileBytes, "application/octet-stream", fileName);
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }

        // GET: TabFile
        public ActionResult Player(int id)
        {
            string currentUserId = User.GetUserId();

            try
            {
                // Do we own the project?
                var tabVersionInDb = (TabVersion)_userAuthenticator.CheckUserReadAccess(Item.TabVersion, id, currentUserId);

                // If we are not the owner, are we a contributor?
                if (tabVersionInDb == null)
                {
                    return Json(new { error = "Tab version not found" });
                }

                ViewBag.Id = id.ToString();
                return View();
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }
    }
}