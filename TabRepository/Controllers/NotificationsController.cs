using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TabRepository.Data;
using TabRepository.Models;

namespace TabRepository.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
        }
        public ActionResult GetNotifications()
        {
            string currentUserId = User.GetUserId();

            try
            {
                var notifications = _context.NotificationUsers.Where(n => n.UserId == currentUserId).Select(n => n.Notification).ToList();
                string html = "";
                int count = 0;

                foreach (Notification notification in notifications)
                {
                    string href = "";
                    switch (notification.NotificationType)
                    {
                        case NotificationType.FriendAccepted: href = Url.Action("Index", "Friends");
                            break;
                        case NotificationType.FriendRequested: href = Url.Action("Index", "Friends");
                            break;
                    }
                    html += "<div class='notification'><a class='btn list-group-item notification-item' href='" + href + "'>" + notification.Message + "<i class='fa fa-times' style='padding-left: 7px;' /></a></div>";
                    count++;
                }
                
                html += "<div class='notification'><a class='clear-btn pull-right' href='#'>Clear</a></div>";

                return Json(new { html, count });
            }
            catch (Exception e)
            {
                return Json(new { });
            }
        }
    }
}