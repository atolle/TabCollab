using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using TabRepository.Data;
using TabRepository.Models;

namespace TabRepository.Controllers
{
    [Authorize]
    public class TabFilesController : Controller
    {
        private ApplicationDbContext _context;

        public TabFilesController(ApplicationDbContext context)
        {
            _context = context;
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
                var tabVersionInDb = (from tabVersion in _context.TabVersions
                                      join tab in _context.Tabs on tabVersion.TabId equals tab.Id
                                      join album in _context.Albums on tab.AlbumId equals album.Id
                                      join project in _context.Projects on album.ProjectId equals project.Id
                                      where project.UserId == currentUserId && id == tabVersion.Id
                                      select tabVersion).Include(u => u.User).FirstOrDefault();

                // If we are not the owner, are we a contributor?
                if (tabVersionInDb == null)
                {
                    tabVersionInDb = (from tabVersion in _context.TabVersions
                                      join tab in _context.Tabs on tabVersion.TabId equals tab.Id
                                      join album in _context.Albums on tab.AlbumId equals album.Id
                                      join project in _context.Projects on album.ProjectId equals project.Id
                                      join contributor in _context.ProjectContributors on project.Id equals contributor.ProjectId
                                      where contributor.UserId == currentUserId && id == tabVersion.Id
                                      select tabVersion).Include(u => u.User).FirstOrDefault();

                    if (tabVersionInDb == null)
                    {
                        return NotFound();
                    }
                }

                var tabFileInDb = _context.TabFiles.Single(f => f.Id == id);

                if (tabFileInDb == null)
                {
                    return NotFound();
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
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        // GET: TabFile
        public ActionResult Player(int id)
        {
            string currentUserId = User.GetUserId();

            try
            {                
                // Do we own the project?
                var tabVersionInDb = (from tabVersion in _context.TabVersions
                                        join tab in _context.Tabs on tabVersion.TabId equals tab.Id
                                        join album in _context.Albums on tab.AlbumId equals album.Id
                                        join project in _context.Projects on album.ProjectId equals project.Id
                                        where project.UserId == currentUserId && id == tabVersion.Id
                                        select tabVersion).Include(u => u.User).FirstOrDefault();

                // If we are not the owner, are we a contributor?
                if (tabVersionInDb == null)
                {
                    tabVersionInDb = (from tabVersion in _context.TabVersions
                                        join tab in _context.Tabs on tabVersion.TabId equals tab.Id
                                        join album in _context.Albums on tab.AlbumId equals album.Id
                                        join project in _context.Projects on album.ProjectId equals project.Id
                                        join contributor in _context.ProjectContributors on project.Id equals contributor.ProjectId
                                        where contributor.UserId == currentUserId && id == tabVersion.Id
                                        select tabVersion).Include(u => u.User).FirstOrDefault();

                    if (tabVersionInDb == null)
                    {
                        return NotFound();
                    }
                }

                ViewBag.Id = id.ToString();
                return View();
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}