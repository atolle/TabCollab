using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TabRespository.Controllers
{
    public class FriendshipsController : Controller
    {
        // GET: Friendships
        public ActionResult Index()
        {
            return View();
        }
    }
}