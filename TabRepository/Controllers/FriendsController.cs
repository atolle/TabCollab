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
                        where u.UserName.Contains(search)
                        select new { u.Id, u.UserName, u.FirstName, u.LastName, f.ActingUserId, f.User1Id, f.User2Id, Status = f.Status == null ? FriendStatus.None : f.Status };

            List < FriendSearchViewModel > viewModel = new List<FriendSearchViewModel>();

            foreach (var user in users)
            {
                var vm = new FriendSearchViewModel();

                vm.Username = user.UserName;
                vm.FirstName = user.FirstName;
                vm.LastName = user.LastName;
                vm.Status = user.Status;
                
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

            try
            {
                if (friendStatus != FriendStatus.Blocked)
                {
                    string currentUserId = User.GetUserId();
                    string otherUserId = _context.Users.Where(u => u.UserName == username).Select(u => u.Id).FirstOrDefault();

                    if (otherUserId == null)
                    {
                        return new StatusCodeResult(StatusCodes.Status404NotFound);
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

                    // Return a null json result as the POST is expecting json
                    return new JsonResult(null);
                }

                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            catch (Exception e)
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }            
        }

        [HttpPost]
        public ActionResult AcceptFriend(string username)
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
                        return new StatusCodeResult(StatusCodes.Status404NotFound);
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

                    // Return a null json result as the POST is expecting json
                    return new JsonResult(null);
                }

                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
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
                        return new StatusCodeResult(StatusCodes.Status404NotFound);
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
                    return new JsonResult(null);
                }

                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
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
                        return new StatusCodeResult(StatusCodes.Status404NotFound);
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
                    return new JsonResult(null);
                }

                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
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
                        return new StatusCodeResult(StatusCodes.Status404NotFound);
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
                    return new JsonResult(null);
                }

                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
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