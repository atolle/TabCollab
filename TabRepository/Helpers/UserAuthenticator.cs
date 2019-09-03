using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TabRepository.Data;
using TabRepository.Interfaces;
using TabRepository.Models;

namespace TabRepository.Helpers
{
    public class UserAuthenticator
    {
        private ApplicationDbContext _context;

        public UserAuthenticator(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<IItem>GetAllItems(Item item, int? parentId, string userId)
        {
            switch (item)
            {
                case Item.Project:

                    var projectInDb = _context.Projects
                        .Include(p => p.User)
                        .Include(p => p.Albums)
                        .ThenInclude(a => a.Tabs)
                        .ThenInclude(t => t.TabVersions)
                        .ThenInclude(v => v.TabFile)
                        .Include(p => p.Contributors)
                        .Where
                        (p =>
                            (
                                // We are the project owner or a contributor
                                p.UserId == userId ||
                                p.Contributors.Where(c => c.UserId == userId).FirstOrDefault() != null
                            )
                        )
                        .ToList();

                    return projectInDb.Cast<IItem>().ToList();

                case Item.Album:

                    var albumInDb = _context.Albums
                        .Include(a => a.User)
                        .Include(a => a.Tabs)
                        .ThenInclude(t => t.TabVersions)
                        .ThenInclude(v => v.TabFile)
                        .Include(a => a.Project)
                        .ThenInclude(p => p.Contributors)
                        .Where
                        (a =>
                            (parentId == 0 ? (1 == 1) : a.ProjectId == parentId) &&
                            (
                                // We are the album owner or a contributor
                                a.UserId == userId ||
                                a.Project.Contributors.Where(c => c.UserId == userId).FirstOrDefault() != null
                            )
                        )
                        .ToList();

                    return albumInDb.Cast<IItem>().ToList();

                case Item.Tab:

                    var tabInDb = _context.Tabs
                        .Include(t => t.User)
                        .Include(t => t.TabVersions)
                        .ThenInclude(v => v.User)
                        .Include(t => t.TabVersions)
                        .ThenInclude(v => v.TabFile)
                        .Include(t => t.Album)
                        .ThenInclude(a => a.Project)
                        .ThenInclude(p => p.Contributors)
                        .Where
                        (t =>
                            (parentId == 0 ? (1 == 1) : t.AlbumId == parentId) &&
                            (
                                // We are the tab owner or a contributor
                                t.UserId == userId ||
                                t.Album.Project.Contributors.Where(c => c.UserId == userId).FirstOrDefault() != null
                            )
                        )
                        .ToList();

                    return tabInDb.Cast<IItem>().ToList();

                case Item.TabVersion:

                    var tabVersionInDb = _context.TabVersions
                        .Include(v => v.User)
                        .Include(v => v.TabFile)
                        .Include(v => v.Tab)
                        .ThenInclude(t => t.Album)
                        .ThenInclude(a => a.Project)
                        .ThenInclude(p => p.Contributors)
                        .Where
                        (v =>
                            (parentId == 0 ? (1 == 1) : v.TabId == parentId) &&
                            (
                                // We are the project owner or a contributor         
                                v.Tab.Album.Project.UserId == userId ||
                                v.Tab.Album.Project.Contributors.Where(c => c.UserId == userId).FirstOrDefault() != null
                            )
                        )
                        .ToList();

                    return tabVersionInDb.Cast<IItem>().ToList();

                default: return null;
            }
        }

        public IItem CheckUserReadAccess(Item item, int? id, string userId)
        {
            switch (item)
            {
                case Item.Project:

                    var projectInDb = _context.Projects
                        .Include(p => p.User)
                        .Include(p => p.Albums)
                        .ThenInclude(a => a.Tabs)
                        .ThenInclude(t => t.TabVersions)
                        .ThenInclude(v => v.TabFile)
                        .Include(p => p.Contributors)                        
                        .Where
                        (p => 
                            p.Id == id &&
                            (
                                // We are the project owner or a contributor
                                p.UserId == userId || 
                                p.Contributors.Where(c => c.UserId == userId).FirstOrDefault() != null
                            )
                        )
                        .FirstOrDefault();

                    return projectInDb as IItem;

                case Item.Album:

                    var albumInDb = _context.Albums
                        .Include(a => a.User)
                        .Include(a => a.Tabs)
                        .ThenInclude(t => t.TabVersions)
                        .ThenInclude(v => v.TabFile)
                        .Include(a => a.Project)
                        .ThenInclude(p => p.Contributors)
                        .Where
                        (a => 
                            a.Id == id && 
                            (
                                // We are the album owner or a contributor
                                a.UserId == userId || 
                                a.Project.Contributors.Where(c => c.UserId == userId).FirstOrDefault() != null
                            )
                        )
                        .FirstOrDefault();

                    return albumInDb as IItem;

                case Item.Tab:

                    var tabInDb = _context.Tabs
                        .Include(t => t.User)
                        .Include(t => t.TabVersions)
                        .ThenInclude(v => v.User)
                        .Include(t => t.TabVersions)
                        .ThenInclude(v => v.TabFile)
                        .Include(t => t.Album)
                        .ThenInclude(a => a.Project)
                        .ThenInclude(p => p.Contributors)
                        .Where
                        (t =>
                            t.Id == id &&
                            (
                                // We are the tab owner or a contributor
                                t.UserId == userId ||
                                t.Album.Project.Contributors.Where(c => c.UserId == userId).FirstOrDefault() != null
                            )
                        )
                        .FirstOrDefault();

                    return tabInDb as IItem;

                case Item.TabVersion:

                    var tabVersionInDb = _context.TabVersions
                        .Include(v => v.User)
                        .Include(v => v.TabFile)
                        .Include(v => v.Tab)
                        .ThenInclude(t => t.Album)
                        .ThenInclude(a => a.Project)
                        .ThenInclude(p => p.Contributors)
                        .Where
                        (v =>
                            v.Id == id &&
                            (
                                // We are the project owner or a contributor         
                                v.Tab.Album.Project.UserId == userId ||
                                v.Tab.Album.Project.Contributors.Where(c => c.UserId == userId).FirstOrDefault() != null
                            )
                        )
                        .FirstOrDefault();

                    return tabVersionInDb as IItem;

                default: return null;
            }
        }

        public IItem CheckUserCreateAccess(Item item, int? id, string userId)
        {
            switch (item)
            {
                case Item.Project:

                    return CheckUserReadAccess(item, id, userId);

                case Item.Album:

                    return CheckUserReadAccess(item, id, userId);

                case Item.Tab:

                    return CheckUserReadAccess(item, id, userId);

                case Item.TabVersion:

                    return CheckUserReadAccess(item, id, userId);

                default: return null;
            }
        }

        public IItem CheckUserEditAccess(Item item, int? id, string userId)
        {
            switch (item)
            {
                case Item.Project:

                    return CheckUserDeleteAccess(item, id, userId);

                case Item.Album:

                    return CheckUserDeleteAccess(item, id, userId);

                case Item.Tab:

                    return CheckUserDeleteAccess(item, id, userId);

                case Item.TabVersion:

                    return CheckUserDeleteAccess(item, id, userId);

                default: return null;
            }
        }

        public IItem CheckUserDeleteAccess(Item item, int? id, string userId)
        {
            switch (item)
            {
                case Item.Project:

                    var projectInDb = _context.Projects
                        .Include(p => p.User)
                        .Include(p => p.Albums)
                        .ThenInclude(a => a.Tabs)
                        .ThenInclude(t => t.TabVersions)
                        .ThenInclude(v => v.TabFile)
                        .Include(p => p.Contributors)
                        .Where
                        (p =>
                            p.Id == id &&
                            (
                                // We are the project owner
                                p.UserId == userId
                            )
                        )
                        .FirstOrDefault();

                    return projectInDb as IItem;

                case Item.Album:

                    var albumInDb = _context.Albums
                        .Include(a => a.User)
                        .Include(a => a.Tabs)
                        .ThenInclude(t => t.TabVersions)
                        .ThenInclude(v => v.TabFile)
                        .Include(a => a.Project)
                        .ThenInclude(p => p.Contributors)
                        .Where
                        (a =>
                            a.Id == id &&
                            (
                                // We are the album owner
                                a.UserId == userId
                            )
                        )
                        .FirstOrDefault();

                    return albumInDb as IItem;

                case Item.Tab:

                    var tabInDb = _context.Tabs
                        .Include(t => t.User)
                        .Include(t => t.TabVersions)
                        .ThenInclude(v => v.User)
                        .Include(t => t.TabVersions)
                        .ThenInclude(v => v.TabFile)
                        .Include(t => t.Album)
                        .ThenInclude(a => a.Project)
                        .ThenInclude(p => p.Contributors)
                        .Where
                        (t =>
                            t.Id == id &&
                            (
                                // We are the tab owner
                                t.UserId == userId
                            )
                        )
                        .FirstOrDefault();

                    return tabInDb as IItem;

                case Item.TabVersion:

                    var tabVersionInDb = _context.TabVersions
                        .Include(v => v.User)
                        .Include(v => v.TabFile)
                        .Include(v => v.Tab)
                        .ThenInclude(t => t.Album)
                        .ThenInclude(a => a.Project)
                        .ThenInclude(p => p.Contributors)
                        .Where
                        (v =>
                            v.Id == id &&
                            (
                                (
                                    // We are a contributor AND tab version owner
                                    v.UserId == userId &&
                                    v.Tab.Album.Project.Contributors.Where(c => c.UserId == userId).FirstOrDefault() != null
                                ) ||
                                // We are the project owner
                                v.Tab.Album.Project.UserId == userId
                            )
                        )
                        .FirstOrDefault();

                    return tabVersionInDb as IItem;

                default: return null;
            }
        }
    }

    public enum Item
    {
        Project,
        Album,
        Tab,
        TabVersion
    }
}
