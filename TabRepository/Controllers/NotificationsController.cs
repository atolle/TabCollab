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
                            href = Url.Action("Dashboard", "Projects");
                            break;
                    }                    
                    html += "<div class='notification' data-notification-id='" + notification.Id + "'><a class='btn list-group-item notification-item' style='display: flex; justify-content: space-between;' href='" + href + "'><span style='overflow: hidden; text-overflow: ellipsis'>" + notification.Message + "</span><i class='fa fa-times notification-delete-btn' data-notification-id='" + notification.Id + "' style='padding-left: 7px; padding-top: 2px;' /></a></div>";
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

        public static void AddNotification(ApplicationDbContext context, NotificationType notificationType, string toUserId, int? projectId, string currentUsername, string currentUserId, string objectName, string parentName)
        {
            string title = "";
            string message = "";

            switch (notificationType)
            {
                case NotificationType.AlbumAdded:
                    title = "Album Added";
                    message = currentUsername + " added " + objectName + " to " + parentName;
                    break;
                case NotificationType.AlbumDeleted:
                    title = "Album Added";
                    message = currentUsername + " deleted " + objectName + " from " + parentName;
                    break;
                case NotificationType.ContributorAdded:
                    title = "Contributor Added";
                    message = currentUsername + " added contributor " + objectName + " to " + parentName;
                    break;
                case NotificationType.FriendAccepted:
                    title = "Friend Accepted";
                    message = currentUsername + " accepted your friend request";
                    break;
                case NotificationType.FriendRequested:
                    title = "Friend Requested";
                    message = currentUsername + " sent you a friend request";
                    break;
                case NotificationType.TabAdded:
                    title = "Tab Added";
                    message = currentUsername + " added " + objectName + " to " + parentName;
                    break;
                case NotificationType.TabDeleted:
                    title = "Tab Deleted";
                    message = currentUsername + " deleted " + objectName + " from " + parentName;
                    break;
                case NotificationType.TabVersionAdded:
                    title = "Tab Version Added";
                    message = currentUsername + " added new version to " + parentName;
                    break;
                case NotificationType.TabVersionDeleted:
                    title = "Tab Version Deleted";
                    message = currentUsername + " deleted version " + objectName + " from " + parentName;
                    break;
            }

            Notification notification = new Notification()
            {
                ToUserId = toUserId,
                FromUserId = currentUserId,
                Title = title,
                Message = message,
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
                    .Where(c => c.ProjectId == projectId && c.UserId != currentUserId)
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
                    .Where(p => p.Id == projectId && p.UserId != currentUserId)
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
                    UserId = toUserId,
                    NotificationId = notification.Id,
                    IsRead = false
                };

                context.NotificationUsers.Add(notificationUser);
            }

            context.SaveChanges();
        }
    }
}