﻿using System.Security.Claims;
using System.Security.Principal;

namespace TabRepository
{
    public static class UserHelpers
    {
        public static string GetUserId(this IPrincipal principal)
        {
            var claimsIdentity = (ClaimsIdentity)principal.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            return claim.Value;
        }

        public static string GetUsername(this IPrincipal principal)
        {
            var claimsIdentity = (ClaimsIdentity)principal.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.Name);
            return claim.Value;
        }
    }
}
