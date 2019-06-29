using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using TabRepository.Data;
using TabRepository.Models;
using TabRepository.ViewModels;

namespace TabRepository.Controllers
{
    [Authorize]
    public class FriendsController : Controller
    {
        private ApplicationDbContext _context;

        public FriendsController(ApplicationDbContext context)
        {
            _context = context; 
        }

        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
        }
        // GET: Friends
        public ActionResult Index()
        {
            return View();
        }

        public ApplicationUser GetCurrentUser()
        {
            string currentUserId = User.GetUserId();

            return _context.Users.FirstOrDefault(u => u.Id == currentUserId);
        }

        [HttpPost]
        public ActionResult FuzzySearch(string searchString)
        {
            var usernames = _context.Users.Where(u => u.UserName.StartsWith(searchString)).Select(n => n.UserName).ToList();
            return Json(usernames);
        }

        [HttpGet]
        public ActionResult Search(string search)
        {
            string currentUserId = User.GetUserId();

            var users = from u in _context.Users
                        from f in _context.Friends.Where(f => (u.Id == f.User1Id && currentUserId == f.User2Id) || (u.Id == f.User2Id && currentUserId == f.User1Id)).DefaultIfEmpty()
                        where u.UserName.StartsWith(search)
                        select new { u.Id, u.UserName, u.FirstName, u.LastName, u.ImageFilePath, f.ActingUserId, f.User1Id, f.User2Id, Status = f.Status == null ? FriendStatus.None : f.Status };

            List < FriendViewModel > viewModel = new List<FriendViewModel>();

            foreach (var user in users)
            {
                var vm = new FriendViewModel();

                vm.Username = user.UserName;
                vm.FirstName = user.FirstName;
                vm.LastName = user.LastName;
                vm.Status = user.Status;
                vm.ImageFilePath = user.ImageFilePath;
                
                if (user.User1Id != null && user.User2Id != null)
                {
                    if (user.ActingUserId == currentUserId)
                    {
                        vm.Direction = Direction.To;
                    }
                    else
                    {
                        vm.Direction = Direction.From;
                    }
                }

                if (user.Id == currentUserId)
                {
                    vm.IsCurrentUser = true;
                }
                else
                {
                    vm.IsCurrentUser = false;
                }

                viewModel.Add(vm);
            }

            return View(viewModel);
        }

        [HttpPost]
        public ActionResult AddFriend(string username)
        {
            var friendStatus = CheckFriendStatus(username);

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (friendStatus != FriendStatus.Blocked)
                    {
                        string currentUserId = User.GetUserId();
                        string currentUsername = User.GetUsername();
                        var otherUserInDb = _context.Users.Where(u => u.UserName == username).FirstOrDefault();
                        string otherUserId = otherUserInDb.Id;
                        var currentUserInDb = _context.Users.Where(u => u.Id == currentUserId).FirstOrDefault();

                        if (currentUserInDb == null || otherUserInDb == null)
                        {
                            return Json(new { error = "User not found" });
                        }

                        var friendInDb = _context
                            .Friends
                            .SingleOrDefault(u => (u.User1Id == currentUserId || u.User2Id == currentUserId) && (u.User1Id == otherUserId || u.User2Id == otherUserId));

                        if (friendInDb == null)
                        {
                            Friend friend = new Friend()
                            {
                                User1Id = currentUserId,
                                User2Id = otherUserId,
                                ActingUserId = currentUserId,
                                Status = FriendStatus.Requested
                            };

                            _context.Friends.Add(friend);
                            _context.SaveChanges();
                        }
                        else
                        {
                            friendInDb.ActingUserId = currentUserId;
                            friendInDb.Status = FriendStatus.Requested;

                            _context.Friends.Update(friendInDb);
                            _context.SaveChanges();
                        }

                        NotificationsController.AddNotification(_context, NotificationType.FriendRequested, otherUserInDb, null, currentUserInDb, null, null);

                        transaction.Commit();

                        // Return a null json result as the POST is expecting json
                        return Json(new { success = true });
                    }

                    return Json(new { error = "Unable to add friend" });
                }
                catch (Exception e)
                {
                    return Json(new { error = e.Message });
                }
            }
        }

        [HttpPost]
        public ActionResult AcceptFriend(string username)
        {
            var friendStatus = CheckFriendStatus(username);

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (friendStatus != FriendStatus.Blocked)
                    {
                        string currentUserId = User.GetUserId();
                        string currentUsername = User.GetUsername();                        
                        var otherUserInDb = _context.Users.Where(u => u.UserName == username).FirstOrDefault();
                        string otherUserId = otherUserInDb.Id;
                        var currentUserInDb = _context.Users.Where(u => u.Id == currentUserId).FirstOrDefault();

                        if (currentUserInDb == null || otherUserInDb == null)
                        {
                            return Json(new { error = "User not found" });
                        }

                        var friendInDb = _context
                            .Friends
                            .SingleOrDefault(u => (u.User1Id == currentUserId || u.User2Id == currentUserId) && (u.User1Id == otherUserId || u.User2Id == otherUserId));

                        if (friendInDb != null)
                        {
                            friendInDb.Status = FriendStatus.Friends;

                            _context.Friends.Update(friendInDb);
                            _context.SaveChanges();
                        }

                        NotificationsController.AddNotification(_context, NotificationType.FriendAccepted, otherUserInDb, null, currentUserInDb, null, null);

                        transaction.Commit();

                        // Return a null json result as the POST is expecting json
                        return Json(new { success = true });
                    }

                    return Json(new { error = "Unable to accept friend" });
                }
                catch (Exception e)
                {
                    return Json(new { error = e.Message });
                }
            }
        }

        [HttpPost]
        public ActionResult DeclineFriend(string username)
        {
            var friendStatus = CheckFriendStatus(username);

            try
            {
                if (friendStatus != FriendStatus.Blocked)
                {
                    string currentUserId = User.GetUserId();
                    string otherUserId = _context.Users.Where(u => u.UserName == username).Select(u => u.Id).FirstOrDefault();

                    if (otherUserId == null)
                    {
                        return Json(new { error = "User not found" });
                    }

                    var friendInDb = _context
                        .Friends
                        .SingleOrDefault(u => (u.User1Id == currentUserId || u.User2Id == currentUserId) && (u.User1Id == otherUserId || u.User2Id == otherUserId));

                    if (friendInDb != null)
                    {
                        friendInDb.Status = FriendStatus.None;

                        _context.Friends.Update(friendInDb);
                        _context.SaveChanges();
                    }

                    // Return a null json result as the POST is expecting json
                    return Json(new { success = true });
                }

                return Json(new { error = "Unable to decline friend" });
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }

        [HttpPost]
        public ActionResult RemoveFriend(string username)
        {
            var friendStatus = CheckFriendStatus(username);

            try
            {
                if (friendStatus != FriendStatus.Blocked)
                {
                    string currentUserId = User.GetUserId();
                    string otherUserId = _context.Users.Where(u => u.UserName == username).Select(u => u.Id).FirstOrDefault();

                    if (otherUserId == null)
                    {
                        return Json(new { error = "User not found" });
                    }

                    var friendInDb = _context
                        .Friends
                        .SingleOrDefault(u => (u.User1Id == currentUserId || u.User2Id == currentUserId) && (u.User1Id == otherUserId || u.User2Id == otherUserId));

                    if (friendInDb != null)
                    {
                        friendInDb.Status = FriendStatus.None;

                        // Friend needs to be removed from all project for which they were a contributor
                        var currentUserProjects = _context.Projects.Where(p => p.UserId == currentUserId).ToList();
                        var otherUserProjects = _context.Projects.Where(p => p.UserId == otherUserId).ToList();

                        // Remove other user from current user's projects
                        if (currentUserProjects != null)
                        {
                            foreach (Project project in currentUserProjects)
                            {
                                var contributorProjects = _context
                                    .ProjectContributors
                                    .Where(c => c.ProjectId == project.Id && c.UserId == otherUserId).ToList();

                                foreach (ProjectContributor contributor in contributorProjects)
                                {
                                    _context.ProjectContributors.Remove(contributor);
                                }
                            }
                        }

                        // Remove current user from other user's projects
                        if (otherUserProjects != null)
                        {
                            foreach (Project project in otherUserProjects)
                            {
                                var contributorProjects = _context
                                    .ProjectContributors
                                    .Where(c => c.ProjectId == project.Id && c.UserId == currentUserId).ToList();

                                foreach (ProjectContributor contributor in contributorProjects)
                                {
                                    _context.ProjectContributors.Remove(contributor);
                                }
                            }
                        }

                        _context.Friends.Update(friendInDb);
                        _context.SaveChanges();
                    }

                    // Return a null json result as the POST is expecting json
                    return Json(new { success = true });
                }

                return Json(new { error = "Unable to remove friend" });
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }

        [HttpPost]
        public ActionResult CancelRequest(string username)
        {
            var friendStatus = CheckFriendStatus(username);

            try
            {
                if (friendStatus != FriendStatus.Blocked)
                {
                    string currentUserId = User.GetUserId();
                    string otherUserId = _context.Users.Where(u => u.UserName == username).Select(u => u.Id).FirstOrDefault();

                    if (otherUserId == null)
                    {
                        return Json(new { error = "User not found" });
                    }

                    var friendInDb = _context
                        .Friends
                        .SingleOrDefault(u => (u.User1Id == currentUserId || u.User2Id == currentUserId) && (u.User1Id == otherUserId || u.User2Id == otherUserId));

                    if (friendInDb != null)
                    {
                        friendInDb.Status = FriendStatus.None;

                        _context.Friends.Update(friendInDb);
                        _context.SaveChanges();
                    }

                    // Return a null json result as the POST is expecting json
                    return Json(new { success = true });
                }

                return Json(new { error = "Unable to cancel request" });
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }

        [HttpGet]
        public ActionResult GetFriendsListPartialView()
        {
            try
            {
                string currentUserId = User.GetUserId();

                var friends = from u in _context.Users
                              from f in _context.Friends.Where(f => (u.Id == f.User1Id && currentUserId == f.User2Id) || (u.Id == f.User2Id && currentUserId == f.User1Id))
                              select new { u.Id, u.UserName, u.FirstName, u.LastName, u.ImageFilePath, f.ActingUserId, f.User1Id, f.User2Id, Status = f.Status == null ? FriendStatus.None : f.Status };

                List<FriendViewModel> viewModel = new List<FriendViewModel>();

                foreach (var user in friends)
                {
                    if (user.Status == FriendStatus.Friends || user.Status == FriendStatus.Requested)
                    {
                        var vm = new FriendViewModel();

                        vm.Username = user.UserName;
                        vm.FirstName = user.FirstName;
                        vm.LastName = user.LastName;
                        vm.Status = user.Status;
                        vm.ImageFilePath = user.ImageFilePath;

                        if (user.User1Id != null && user.User2Id != null)
                        {
                            if (user.ActingUserId == currentUserId)
                            {
                                vm.Direction = Direction.To;
                            }
                            else
                            {
                                vm.Direction = Direction.From;
                            }
                        }

                        if (user.Id == currentUserId)
                        {
                            vm.IsCurrentUser = true;
                        }
                        else
                        {
                            vm.IsCurrentUser = false;
                        }

                        viewModel.Add(vm);
                    }
                }

                return PartialView("_FriendsList", viewModel);
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }

        private FriendStatus CheckFriendStatus(string username)
        {
            string currentUserId = User.GetUserId();

            var friendStatus = _context
                .Friends                
                .Where(u => (u.User1Id == currentUserId || u.User2Id == currentUserId) && (u.User1.UserName == username || u.User2.UserName == username))
                .Select(u => u.Status);

            if (friendStatus.Any())
            {
                return friendStatus.Single();
            }

            return FriendStatus.None;
        }            
    }
}