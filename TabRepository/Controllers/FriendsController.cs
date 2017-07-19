using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TabRespository.Controllers
{
    public class FriendsController : Controller
    {
        // GET: Friends
        public ActionResult Index()
        {
            return View();
        }
    }
}