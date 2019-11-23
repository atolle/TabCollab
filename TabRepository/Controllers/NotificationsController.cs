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
                var notifications = _context.Notifications
                    .Where(n => n.ToUserId == currentUserId && n.IsRead == false)
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
                        case NotificationType.SubscriptionStatusUpdated:
                        case NotificationType.AccountTypeChanged:
                            href = Url.Action("Index", "Account");
                            break;
                        case NotificationType.InvoiceCreated:
                        case NotificationType.InvoicePaid:
                        case NotificationType.InvoicePaymentFailed:
                        case NotificationType.InvoiceUpdated:
                            href = Url.Action("Billing", "Account");
                            break;
                    }

                    string imagePath = "/images/TabCollab_icon_white_blackcircle_512.png";

                    if (notification.FromUser != null && notification.FromUser.ImageFilePath != null)
                    {
                        imagePath = notification.FromUser.ImageFilePath;
                    }                    

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
                                        "<i class='fa fa-times fa-lg notification-read-btn' data-notification-id='" + notification.Id + "'/>" +
                                    "</div>" +                                                                        
                                "</a>" + 
                            "</div>";
                    count++;
                }
                
                html += "<div class='notification notification-read-all-btn'><a class='clear-btn pull-right' href='#'>Clear</a></div>";

                return Json(new { html, count });
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }

        public ActionResult ReadNotification(int notificationId)
        {
            string currentUserId = User.GetUserId();

            try
            {
                var notification = _context.Notifications.Where(n => n.ToUserId == currentUserId && n.Id == notificationId).FirstOrDefault();

                if (notification != null)
                {
                    notification.IsRead = true;

                    _context.Notifications.Update(notification);

                    _context.SaveChanges();
                }

                return Json(new { success = true });
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message }); 
            }
        }

        public ActionResult ReadAllNotifications()
        {
            string currentUserId = User.GetUserId();

            try
            {
                var notifications = _context.Notifications.Where(n => n.ToUserId == currentUserId && n.IsRead == false).ToList();

                if (notifications != null)
                {
                    foreach (var notification in notifications)
                    {
                        notification.IsRead = true;

                        _context.Notifications.Update(notification);
                    }

                    _context.SaveChanges();
                }

                return Json(new { success = true });
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }

        public static void AddNotification(
            ApplicationDbContext context, 
            NotificationType notificationType, 
            ApplicationUser toUser, 
            int? projectId, 
            ApplicationUser fromUser, 
            string objectName, 
            string parentName
        )
        {
            string title = "";
            string message1 = "";
            string message2 = "";

            switch (notificationType)
            {
                case NotificationType.ProjectAdded:
                    title = "Project Added";
                    message1 = "Project Added: " + objectName;
                    break;
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
                    message1 = "Friend Accepted: " + fromUser.UserName;
                    break;
                case NotificationType.FriendRequested:
                    title = "Friend Request";
                    message1 = "Friend Request: " + fromUser.UserName;
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
                case NotificationType.SubscriptionStatusUpdated:
                    title = "Subscription Status Changed";
                    message1 = "Subscription status changed to " + objectName;
                    break;
                case NotificationType.InvoiceCreated:
                    title = "Invoice Created";
                    message1 = "A new invoice has been created";
                    break;
                case NotificationType.InvoicePaid:
                    title = "Invoice Paid";
                    message1 = "An invoice has been paid";
                    break;
                case NotificationType.InvoicePaymentFailed:
                    title = "Invoice Payment Failed";
                    message1 = "An invoice payment has failed";
                    break;
                case NotificationType.InvoiceUpdated:
                    title = "Invoice Updated";
                    message1 = "An invoice has been updated";
                    break;
                case NotificationType.AccountTypeChanged:
                    title = "Account Type Changed";
                    message1 = "Account type changed to " + objectName;
                    break;
            }

            // If toUser is null then this is an update that should be going out to contributors
            if (toUser == null)
            {
                if (projectId != null)
                {
                    var contributors = context
                        .ProjectContributors
                        .Where(c => c.ProjectId == projectId && c.UserId != fromUser.Id)
                        .ToList();

                    foreach (ProjectContributor contributor in contributors)
                    {
                        Notification notification = new Notification()
                        {
                            ToUserId = contributor.UserId,
                            FromUserId = fromUser == null ? null : fromUser.Id,
                            Title = title,
                            Message1 = message1,
                            Message2 = message2,
                            Timestamp = DateTime.Now,
                            ProjectId = projectId,
                            NotificationType = notificationType
                        };

                        context.Notifications.Add(notification);
                    }

                    var ownerId = context
                        .Projects
                        .Where(p => p.Id == projectId && p.UserId != fromUser.Id)
                        .Select(p => p.UserId)
                        .FirstOrDefault();

                    if (ownerId != null)
                    {
                        Notification notification = new Notification()
                        {
                            ToUserId = ownerId,
                            FromUserId = fromUser == null ? null : fromUser.Id,
                            Title = title,
                            Message1 = message1,
                            Message2 = message2,
                            Timestamp = DateTime.Now,
                            ProjectId = projectId,
                            NotificationType = notificationType
                        };

                        context.Notifications.Add(notification);
                    }

                    context.SaveChanges();
                }
            }
            else
            {
                Notification notification = new Notification()
                {
                    ToUserId = toUser.Id,
                    FromUserId = fromUser == null ? null : fromUser.Id,
                    Title = title,
                    Message1 = message1,
                    Message2 = message2,
                    Timestamp = DateTime.Now,
                    ProjectId = projectId,
                    NotificationType = notificationType
                };

                context.Notifications.Add(notification);
                context.SaveChanges();
            }
        }
    }
}