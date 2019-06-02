using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TabRepository.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error(int code)
        {
            switch (code)
            {
                case 404:
                    ViewData["Header"] = "Page Not Found";
                    ViewData["Body"] = "We couldn't find the page you requested. Please check your URL.";
                    break;
                default:
                    ViewData["Header"] = "Error Occurred";
                    ViewData["Body"] = "An error has occurred. Please try your request again in a few minutes.";
                    break;
            }
            return View();
        }

        public IActionResult Help()
        {
            return View();
        }

        public IActionResult TermsOfService()
        {
            return View();
        }

        public IActionResult PrivacyPolicy()
        {
            return View();
        }

        public IActionResult GetTermsOfService()
        {
            return PartialView("_TermsOfService");
        }

        public IActionResult GetPrivacyPolicy()
        {
            return PartialView("_PrivacyPolicy");
        }
    }
}
