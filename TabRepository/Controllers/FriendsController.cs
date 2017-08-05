using Microsoft.AspNetCore.Mvc;
using System.Linq;
using TabRepository.Data;

namespace TabRespository.Controllers
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
        public ActionResult Search(string searchString)
        {
            var usernames = _context.Users.Where(u => u.UserName.StartsWith(searchString)).Select(n => n.UserName).ToList();
            return Json(usernames);
        }
    }
}