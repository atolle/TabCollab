using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using TabRepository.Data;
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

        [HttpPost]
        public ActionResult FuzzySearch(string searchString)
        {
            var usernames = _context.Users.Where(u => u.UserName.StartsWith(searchString)).Select(n => n.UserName).ToList();
            return Json(usernames);
        }

        [HttpGet]
        public ActionResult Search(string search)
        {
            var users = _context.Users.Where(u => u.UserName.StartsWith(search)).ToList();
            List<FriendSearchViewModel> viewModel = new List<FriendSearchViewModel>();

            foreach (var user in users)
            {
                var vm = new FriendSearchViewModel()
                {
                    Username = user.UserName,
                    FirstName = user.FirstName,
                    LastName = user.LastName
                };

                viewModel.Add(vm);
            }

            return View(viewModel);
        }
    }
}