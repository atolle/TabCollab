using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TabRepository.Data;
using TabRepository.Dtos;
using TabRepository.Models;
using TabRepository.ViewModels;

namespace TabRepository.Controllers.Api
{
    [Produces("application/json")]
    [Route("api/TabVersions")]
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

        [HttpGet("{id}", Name = "GetTabVersions")]
        public IActionResult GetTabVersions(int id)
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

                var tabVersionsInDb = (from tabVersion in _context.TabVersions
                                       join tabFile in _context.TabFiles on tabVersion.Id equals tabFile.TabVersion.Id
                                       where tabVersion.Tab.Id == tabInDb.Id
                                       select new TabVersionDto
                                       {
                                           Id = tabVersion.Id,
                                           TabFileDto = Mapper.Map<TabFile, TabFileDto>(tabVersion.TabFile),
                                           TabId = tabVersion.Tab.Id,
                                           UserId = tabVersion.UserId,
                                           Description = tabVersion.Description,
                                           Version = tabVersion.Version,
                                           DateCreated = tabVersion.DateCreated,
                                           IsOwner = tabInDb.UserId == currentUserId || tabVersion.UserId == currentUserId
                                       });

                var albumInDb = _context.Tabs.Include(t => t.Album).SingleOrDefault(t => t.Id == id).Album;

                if (tabVersionsInDb == null || tabInDb == null || albumInDb == null)
                {
                    return NotFound();
                }

                return Ok(JsonConvert.SerializeObject(tabVersionsInDb));
            }
            catch (Exception e)
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}