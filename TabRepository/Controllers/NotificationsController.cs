using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
                var notifications = _context.NotificationUsers
                    .Where(n => n.UserId == currentUserId)
                    .Select(n => n.Notification)
                    .Include(n => n.FromUser)
                    .OrderByDescending(n => n.Timestamp)
                    .ToList();

                string html = "";
                int count = 0;

                foreach (Notification notification in notifications)
                {
                    string href = "";
                    switch (notification.NotificationType)
                    {
                        case NotificationType.FriendAccepted: 
                        case NotificationType.FriendRequested:
                            href = Url.Action("Index", "Friends");
                            break;
                        case NotificationType.AlbumAdded:
                        case NotificationType.AlbumDeleted:
                            href = Url.Action("Index", "Albums");
                            break;
                        case NotificationType.TabAdded:
                        case NotificationType.TabDeleted:
                        case NotificationType.TabVersionAdded:
                        case NotificationType.TabVersionDeleted:
                        case NotificationType.ContributorAdded:
                            href = Url.Action("Index", "Tabs");
                            break;
                    }

                    string imagePath = notification.FromUser.ImageFilePath == null ? "/images/TabCollab_icon_white_blackcircle_512.png" : notification.FromUser.ImageFilePath;

                    html += "<div class='notification' data-notification-id='" + notification.Id + "'> " +
                                "<a class='list-group-item notification-item' href='" + href + "'>" + 
                                    "<div style='display: flex; justify-content: center; width: 60px'>" + 
                                        "<img class='notification-image' src=" + imagePath + ">" +
                                    "</div>" +
                                    "<div style='width: calc(100% - 90px)'>" +
                                        "<div class='notification-message'>"+ notification.Message1 + "</div>" +
                                        "<div class='notification-message'>" + notification.Message2 + "</div>" +
                                    "</div>" +
                                    "<div style='display: flex; justify-content: center; width: 30px'>" +
                                        "<i class='fa fa-times fa-lg notification-delete-btn' data-notification-id='" + notification.Id + "'/>" +
                                    "</div>" +                                                                        
                                "</a>" + 
                            "</div>";
                    count++;
                }
                
                html += "<div class='notification notification-delete-all-btn'><a class='clear-btn pull-right' href='#'>Clear</a></div>";

                return Json(new { html, count });
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
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

                return Json(new { success = true });
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message }); 
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

                return Json(new { success = true });
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }

        public static void AddNotification(ApplicationDbContext context, NotificationType notificationType, ApplicationUser toUser, int? projectId, ApplicationUser currentUser, string objectName, string parentName)
        {
            string title = "";
            string message1 = "";
            string message2 = "";

            switch (notificationType)
            {
                case NotificationType.AlbumAdded:
                    title = "Album Added";
                    message1 = "Album Added: " + objectName;
                    message2 = "Project: " + parentName;
                    break;
                case NotificationType.AlbumDeleted:
                    title = "Album Deleted";
                    message1 = "Album Deleted: " + objectName;
                    message2 = "Project: " + parentName;
                    break;
                case NotificationType.ContributorAdded:
                    title = "Contributor Added";
                    message1 = "Contributor Added: " + objectName;
                    message2 = "Project: " + parentName;
                    break;
                case NotificationType.FriendAccepted:
                    title = "Friend Accepted";
                    message1 = "Friend Accepted: " + currentUser.UserName;
                    break;
                case NotificationType.FriendRequested:
                    title = "Friend Request";
                    message1 = "Friend Request: " + currentUser.UserName;
                    break;
                case NotificationType.TabAdded:
                    title = "Tab Added";
                    message1 = "Tab Added: " + objectName;
                    message2 = "Album: " + parentName;
                    break;
                case NotificationType.TabDeleted:
                    title = "Tab Deleted";
                    message1 = "Tab Deleted: " + objectName;
                    message2 = "Album: " + parentName;
                    break;
                case NotificationType.TabVersionAdded:
                    title = "Tab Version Added";
                    message1 = "Tab Version Added";
                    message2 = "Tab: " + parentName;
                    break;
                case NotificationType.TabVersionDeleted:
                    title = "Tab Version Deleted";
                    message1 = "Tab Version Deleted";
                    message2 = "Tab: " + parentName;
                    break;
            }

            Notification notification = new Notification()
            {
                ToUserId = toUser == null ? null : toUser.Id,
                FromUserId = currentUser.Id,
                Title = title,
                Message1 = message1,
                Message2 = message2,
                Timestamp = DateTime.Now,
                ProjectId = projectId,
                NotificationType = notificationType
            };

            context.Notifications.Add(notification);
            context.SaveChanges();

            if (projectId != null)
            {
                var contributors = context
                    .ProjectContributors
                    .Where(c => c.ProjectId == projectId && c.UserId != currentUser.Id)
                    .ToList();                

                foreach (ProjectContributor contributor in contributors)
                {
                    NotificationUser notificationUser = new NotificationUser()
                    {
                        UserId = contributor.UserId,
                        NotificationId = notification.Id,
                        IsRead = false
                    };

                    context.NotificationUsers.Add(notificationUser);
                }

                var ownerId = context
                    .Projects
                    .Where(p => p.Id == projectId && p.UserId != currentUser.Id)
                    .Select(p => p.UserId)
                    .FirstOrDefault();

                if (ownerId != null)
                {
                    NotificationUser notificationUser = new NotificationUser()
                    {
                        UserId = ownerId,
                        NotificationId = notification.Id,
                        IsRead = false
                    };

                    context.NotificationUsers.Add(notificationUser);
                }
            }
            else
            {
                NotificationUser notificationUser = new NotificationUser()
                {
                    UserId = toUser.Id,
                    NotificationId = notification.Id,
                    IsRead = false
                };

                context.NotificationUsers.Add(notificationUser);
            }

            context.SaveChanges();
        }
    }
}