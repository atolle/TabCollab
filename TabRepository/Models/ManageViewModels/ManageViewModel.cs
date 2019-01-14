using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

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

        public string Username { get; set; }

        public string Firstname { get; set; }

        public string Lastname { get; set; }

        public DateTime? SubsriptionExpiration { get; set; }

        public int TabVersionCount { get; set; }
    }
}
