using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TabRepository.Models;
using TabRepository.Models.ManageViewModels;
using TabRepository.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using TabRepository.Helpers;
using TabRepository.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using TabRepository.Models.AccountViewModels;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace TabRepository.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly string _externalCookieScheme;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;
        private readonly ILogger _logger;
        private ApplicationDbContext _context;
        private FileUploader _fileUploader;
        private readonly IHostingEnvironment _appEnvironment;
        private IConfiguration _configuration;

        public ManageController(
          UserManager<ApplicationUser> userManager,
          SignInManager<ApplicationUser> signInManager,
          IEmailSender emailSender,
          ISmsSender smsSender,
          ILoggerFactory loggerFactory,
          ApplicationDbContext context, 
          IHostingEnvironment appEnvironment,
          IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = loggerFactory.CreateLogger<ManageController>();
            _context = context;
            _appEnvironment = appEnvironment;
            _fileUploader = new FileUploader(context, appEnvironment);
            _configuration = configuration;
        }

        //
        // GET: /Manage/Index
        [HttpGet]
        public async Task<IActionResult> Index(ManageMessageId? message = null)
        {
            try
            {
                string currentUserId = User.GetUserId();

                ViewData["StatusMessage"] =
                    message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                    : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                    : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                    : message == ManageMessageId.Error ? "An error has occurred."
                    : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
                    : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                    : "";

                var user = await GetCurrentUserAsync();
                if (user == null)
                {
                    return View("Error");
                }

                // Get a count of total tab versions that this user owns (i.e. their projects)
                var tabVersionCount = _context.TabVersions.Include(u => u.User)
                    .Include(v => v.Tab)
                    .Include(v => v.Tab.Album)
                    .Include(v => v.Tab.Album.Project)
                    .Where(v => v.Tab.Album.Project.UserId == currentUserId)
                    .Count();
               
                SubscriptionStatus subscriptionStatus = SubscriptionStatus.None;
                string creditCardLastFour = null;
                string creditCardExpiration = null;

                Models.AccountViewModels.AccountType accountType = _context.Users.Where(u => u.Id == currentUserId).Select(u => u.AccountType).FirstOrDefault();

                var customerInDb = _context.StripeCustomers.Where(c => c.UserId == currentUserId).FirstOrDefault();
                var subscriptionInDb = _context.StripeSubscriptions.Where(s => s.Status.ToLower() == "active" && s.CustomerId == user.CustomerId).FirstOrDefault();

                if (subscriptionInDb != null)
                {
                    if (subscriptionInDb.CancelAtPeriodEnd)
                    {
                        subscriptionStatus = SubscriptionStatus.CancelAtPeriodEnd;
                    }
                    else
                    {
                        subscriptionStatus = SubscriptionStatus.Active;
                    }
                }

                if (customerInDb != null)
                {
                    var customer = StripeProcessor.GetCustomer(_configuration, customerInDb);

                    creditCardLastFour = (customer.Sources.FirstOrDefault() as Card).Last4;
                    creditCardExpiration = (customer.Sources.FirstOrDefault() as Card).ExpMonth + "/" + (customer.Sources.FirstOrDefault() as Card).ExpYear;
                }

                var model = new ManageViewModel
                {
                    HasPassword = await _userManager.HasPasswordAsync(user),
                    PhoneNumber = await _userManager.GetPhoneNumberAsync(user),
                    TwoFactor = await _userManager.GetTwoFactorEnabledAsync(user),
                    Logins = await _userManager.GetLoginsAsync(user),
                    BrowserRemembered = await _signInManager.IsTwoFactorClientRememberedAsync(user),
                    Username = user.UserName,
                    Firstname = user.FirstName,
                    Lastname = user.LastName,
                    ImageFileName = user.ImageFileName,
                    ImageFilePath = user.ImageFilePath,
                    SubsriptionExpiration = user.SubscriptionExpiration,
                    TabVersionCount = tabVersionCount,
                    Email = user.Email,
                    SubscriptionStatus = subscriptionStatus,
                    AccountType = accountType,
                    CreditCardExpiration = creditCardExpiration,
                    CreditCardLast4 = creditCardLastFour
                };
                return View(model);
            }
            catch (Exception e)
            {
                return View("Error");
            }
        }

        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLogin(RemoveLoginViewModel account)
        {
            ManageMessageId? message = ManageMessageId.Error;
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var result = await _userManager.RemoveLoginAsync(user, account.LoginProvider, account.ProviderKey);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    message = ManageMessageId.RemoveLoginSuccess;
                }
            }
            return RedirectToAction(nameof(ManageLogins), new { Message = message });
        }

        //
        // GET: /Manage/AddPhoneNumber
        public IActionResult AddPhoneNumber()
        {
            return View();
        }

        //
        // POST: /Manage/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // Generate the token and send it
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            var code = await _userManager.GenerateChangePhoneNumberTokenAsync(user, model.PhoneNumber);
            await _smsSender.SendSmsAsync(model.PhoneNumber, "Your security code is: " + code);
            return RedirectToAction(nameof(VerifyPhoneNumber), new { PhoneNumber = model.PhoneNumber });
        }

        //
        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableTwoFactorAuthentication()
        {
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                await _userManager.SetTwoFactorEnabledAsync(user, true);
                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation(1, "User enabled two-factor authentication.");
            }
            return RedirectToAction(nameof(Index), "Manage");
        }

        //
        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableTwoFactorAuthentication()
        {
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                await _userManager.SetTwoFactorEnabledAsync(user, false);
                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation(2, "User disabled two-factor authentication.");
            }
            return RedirectToAction(nameof(Index), "Manage");
        }

        //
        // GET: /Manage/VerifyPhoneNumber
        [HttpGet]
        public async Task<IActionResult> VerifyPhoneNumber(string phoneNumber)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            var code = await _userManager.GenerateChangePhoneNumberTokenAsync(user, phoneNumber);
            // Send an SMS to verify the phone number
            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        //
        // POST: /Manage/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var result = await _userManager.ChangePhoneNumberAsync(user, model.PhoneNumber, model.Code);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction(nameof(Index), new { Message = ManageMessageId.AddPhoneSuccess });
                }
            }
            // If we got this far, something failed, redisplay the form
            ModelState.AddModelError(string.Empty, "Failed to verify phone number");
            return View(model);
        }

        //
        // POST: /Manage/RemovePhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePhoneNumber()
        {
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var result = await _userManager.SetPhoneNumberAsync(user, null);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction(nameof(Index), new { Message = ManageMessageId.RemovePhoneSuccess });
                }
            }
            return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });
        }

        //
        // GET: /Manage/ChangePassword
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation(3, "User changed their password successfully.");
                    return PartialView("_ChangePasswordConfirmation");
                }
                AddErrors(result);
                // If we got this far, something failed, redisplay form
                return PartialView("_ChangePasswordForm", model);
            }
            // If we got this far, something failed, redisplay form
            return PartialView("_ChangePasswordForm", model);
        }

        //
        // GET: /Manage/SetPassword
        [HttpGet]
        public IActionResult SetPassword()
        {
            return View();
        }

        //
        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var result = await _userManager.AddPasswordAsync(user, model.NewPassword);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction(nameof(Index), new { Message = ManageMessageId.SetPasswordSuccess });
                }
                AddErrors(result);
                return View(model);
            }
            return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });
        }

        //GET: /Manage/ManageLogins
        [HttpGet]
        public async Task<IActionResult> ManageLogins(ManageMessageId? message = null)
        {
            ViewData["StatusMessage"] =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.AddLoginSuccess ? "The external login was added."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            var userLogins = await _userManager.GetLoginsAsync(user);
            var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
            var otherLogins = schemes.Where(auth => userLogins.All(ul => auth.Name != ul.LoginProvider)).ToList();
            ViewData["ShowRemoveButton"] = user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }

        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkLogin(string provider)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // Request a redirect to the external login provider to link a login for the current user
            var redirectUrl = Url.Action(nameof(LinkLoginCallback), "Manage");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, _userManager.GetUserId(User));
            return Challenge(properties, provider);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ManageViewModel viewModel)
        {
            if (!ModelState.IsValid)    // If not valid, set the view model to current customer
            {                           // initialize membershiptypes and pass it back to same view
                // Need to return JSON failure to form
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            string currentUserId = User.GetUserId();

            var user = _context.Users.SingleOrDefault(u => u.Id == currentUserId);

            // Limit file size to 1 MB
            if (viewModel.Image != null)
            {
                if (viewModel.Image.Length > 1000000)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Image size limit is 1 MB");
                }

                string imageFilePath = await _fileUploader.UploadFileToFileSystem(viewModel.Image, currentUserId, "Profile");
                user.ImageFileName = viewModel.Image.FileName;
                user.ImageFilePath = imageFilePath;
            }

            user.FirstName = viewModel.Firstname;
            user.LastName = viewModel.Lastname;
            user.Email = viewModel.Email;

            _context.SaveChanges();

            return new JsonResult(new { user.FirstName, user.LastName, user.Email, user.ImageFilePath });
        }
        // POST: Projects
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProfileImage(ManageViewModel viewModel)
        {
            if (!ModelState.IsValid)    // If not valid, set the view model to current customer
            {                           // initialize membershiptypes and pass it back to same view
                // Need to return JSON failure to form
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            // Limit file size to 1 MB
            if (viewModel.Image.Length > 1000000)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Image size limit is 1 MB");
            }

            string currentUserId = User.GetUserId();

            var user = _context.Users.SingleOrDefault(u => u.Id == currentUserId);

            string imageFilePath = await _fileUploader.UploadFileToFileSystem(viewModel.Image, currentUserId, "Profile");

            user.ImageFileName = viewModel.Image.FileName;
            user.ImageFilePath = imageFilePath;

            _context.SaveChanges();

            return new JsonResult(new { user.ImageFilePath }); 
        }

        //
        // GET: /Manage/LinkLoginCallback
        [HttpGet]
        public async Task<ActionResult> LinkLoginCallback()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            var info = await _signInManager.GetExternalLoginInfoAsync(await _userManager.GetUserIdAsync(user));
            if (info == null)
            {
                return RedirectToAction(nameof(ManageLogins), new { Message = ManageMessageId.Error });
            }
            var result = await _userManager.AddLoginAsync(user, info);
            var message = ManageMessageId.Error;
            if (result.Succeeded)
            {
                message = ManageMessageId.AddLoginSuccess;
                // Clear the existing external cookie to ensure a clean login process
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            }
            return RedirectToAction(nameof(ManageLogins), new { Message = message });
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            AddLoginSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

        private Task<ApplicationUser> GetCurrentUserAsync()
        {
            return _userManager.GetUserAsync(HttpContext.User);
        }

        #endregion
    }
}
