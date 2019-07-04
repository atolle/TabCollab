using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using TabRepository.Models.AccountViewModels;

namespace TabRepository.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Bio { get; set; }

        public string ImageFileName { get; set; }

        public string ImageFilePath { get; set; }

        public bool TabTutorialShown { get; set; }

        public DateTime? SubscriptionExpiration { get; set; }

        public AccountType AccountType { get; set; }

        public string CustomerId { get; set; }

        public virtual StripeCustomer Customer { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            var authenticationType = "Put authentication type Here";
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = new ClaimsIdentity(await manager.GetClaimsAsync(this), authenticationType);
            // Add custom user claims here
            return userIdentity;
        }
    }
}
