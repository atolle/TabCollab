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
        public ActionResult GetNotificationPanel()
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
                    html += "<div class='notification' data-notification-id='" + notification.Id + "'><a class='btn list-group-item notification-item' href='" + href + "'>" + notification.Message + "<i class='fa fa-times notification-delete-btn' data-notification-id='" + notification.Id + "' style='padding-left: 7px;' /></a></div>";
                    count++;
                }
                
                html += "<div class='notification notification-delete-all-btn'><a class='clear-btn pull-right' href='#'>Clear</a></div>";

                return Json(new { html, count });
            }
            catch (Exception e)
            {
                return Json(new { });
            }
        }

        public ActionResult DeleteNotificationForUser(int notificationId)
        {
            string currentUserId = User.GetUserId();

            try
            {
                var notification = _context.NotificationUsers.Where(n => n.UserId == currentUserId && n.Notification.Id == notificationId).FirstOrDefault();

                if (notification != null)
                {
                    _context.NotificationUsers.Remove(notification);

                    _context.SaveChanges();
                }

                return Json(new { });
            }
            catch
            {
                return Json(new { });
            }
        }

        public ActionResult DeleteAllNotificationsForUser()
        {
            string currentUserId = User.GetUserId();

            try
            {
                var notifications = _context.NotificationUsers.Where(n => n.UserId == currentUserId).ToList();

                if (notifications != null)
                {
                    _context.NotificationUsers.RemoveRange(notifications);

                    _context.SaveChanges();
                }

                return Json(new { });
            }
            catch
            {
                return Json(new { });
            }
        }
    }
}