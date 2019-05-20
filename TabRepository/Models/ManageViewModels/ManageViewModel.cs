using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using TabRepository.Models.AccountViewModels;

namespace TabRepository.Models.ManageViewModels
{
    public class ManageViewModel
    {
        public bool HasPassword { get; set; }

        public IList<UserLoginInfo> Logins { get; set; }

        public string PhoneNumber { get; set; }

        public bool TwoFactor { get; set; }

        public bool BrowserRemembered { get; set; }

        public string ImageFilePath { get; set; }

        public string ImageFileName { get; set; }

        public IFormFile Image { get; set; }

        [FileExtensions(Extensions = "png,gif,jpeg,jpg,nofile", ErrorMessage = "Invalid file type")]
        public string FileName
        {
            get
            {
                if (Image != null)
                    return Image.FileName;
                else
                    return ".nofile";
            }
        }

        public string Username { get; set; }

        public string Firstname { get; set; }

        public string Lastname { get; set; }

        public DateTime? SubsriptionExpiration { get; set; }

        public int TabVersionCount { get; set; }

        [Required]
        public string Email { get; set; }

        public bool HasActiveSubscription { get; set; }

        public SubscriptionStatus SubscriptionStatus { get; set; }

        public AccountType AccountType { get; set; }

        public string CreditCardExpiration { get; set; }

        public string CreditCardLast4 { get; set; }
    }
}
