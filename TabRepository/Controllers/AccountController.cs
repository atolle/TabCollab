﻿using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TabRepository.Models;
using TabRepository.Models.AccountViewModels;
using TabRepository.Services;
using Microsoft.AspNetCore.Authentication;
using TabRepository.Data;
using TabRepository.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System;
using TabRepository.ViewModels;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace TabRepository.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;
        private readonly ILogger _logger;
        private readonly string _externalCookieScheme;
        private ApplicationDbContext _context;
        private FileUploader _fileUploader;
        private readonly IHostingEnvironment _appEnvironment;
        private IConfiguration _configuration;

        public AccountController(
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
            _logger = loggerFactory.CreateLogger<AccountController>();
            _context = context;
            _appEnvironment = appEnvironment;
            _fileUploader = new FileUploader(context, appEnvironment);
            _configuration = configuration;
        }

        //
        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {          
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                // Clear the existing external cookie to ensure a clean login process
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    var userInDb = _context.Users.Where(u => u.UserName.ToLower() == model.Username.ToLower()).FirstOrDefault();
                    var subscriptionInDb = _context.StripeSubscriptions.Where(s => (s.Status == "active" || s.Status == "trialing") && s.CustomerId == _context.StripeCustomers.Where(c => c.UserId == userInDb.Id).Select(c => c.Id).FirstOrDefault()).FirstOrDefault();                    

                    if (subscriptionInDb != null)
                    {
                        var subscription = StripeProcessor.GetSubscription(_configuration, subscriptionInDb);

                        // If the subscription is no longer active, change to free account
                        if (subscription.Status.ToLower() != "active" && subscription.Status.ToLower() != "trialing")
                        {
                            userInDb.AccountType = AccountType.Free;
                            _context.SaveChanges();
                        }
                    }
                    else
                    { 
                        userInDb.AccountType = AccountType.Free;
                        _context.SaveChanges();
                    }

                    _logger.LogInformation(1, "User logged in.");
                    return RedirectToLocal(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning(2, "User account locked out.");
                    return View("Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/SubscriptionConfirmation
        //[HttpGet]
        //[AllowAnonymous]
        //public async Task<IActionResult> SubscriptionConfirmation(string subscription_id)
        //{
        //    try
        //    {
        //        var subscriptionInDb = _context.PayPalSubscriptions.Where(a => a.Id == subscription_id).FirstOrDefault();

        //        if (subscriptionInDb == null)
        //        {
        //            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        //        }

        //        string requestToken = await PayPalProcessor.GetPayPalToken(_configuration);
        //        var subscriptionJson = await PayPalProcessor.GetSubscription(requestToken, subscriptionInDb.Id);
        //        var subscriptionObject = JObject.Parse(subscriptionJson);

        //        subscriptionInDb.Status = (string)subscriptionObject["status"];

        //        // User confirmed their subscription, so update their account type and subscription expiration
        //        var userInDb = _context.Users.SingleOrDefault(u => u.Id == subscriptionInDb.UserId);

        //        if (userInDb == null)
        //        {
        //            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        //        }

        //        if (subscriptionInDb.Status.ToLower() == "active")
        //        {
        //            userInDb.AccountType = AccountType.Subscription;

        //            // Set their subscription expiration
        //            if (!userInDb.SubscriptionExpiration.HasValue)
        //            {
        //                userInDb.SubscriptionExpiration = DateTime.Now.AddYears(1).AddDays(1);
        //            }
        //            else
        //            {
        //                if (userInDb.SubscriptionExpiration < DateTime.Now)
        //                {
        //                    userInDb.SubscriptionExpiration = DateTime.Now.AddYears(1).AddDays(1);
        //                }
        //                else
        //                {
        //                    userInDb.SubscriptionExpiration = userInDb.SubscriptionExpiration.Value.AddYears(1).AddDays(1);
        //                }
        //            }

        //            _context.SaveChanges();

        //            return View();
        //        }
        //        else
        //        {
        //            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        //        }

        //    }
        //    catch (Exception e)
        //    {
        //        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        //    }
        //}

        //
        // GET: /Account/SubscriptionCancel
        //[HttpPost]
        //public async Task<IActionResult> SubscriptionCancel()
        //{
        //    try
        //    {
        //        string currentUserId = User.GetUserId();

        //        var userInDb = _context.Users.Where(u => u.Id == currentUserId).FirstOrDefault();

        //        var subscriptionInDb = _context.PayPalSubscriptions.Where(a => a.UserId == currentUserId).FirstOrDefault();

        //        if (subscriptionInDb == null)
        //        {
        //            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        //        }              

        //        string requestToken = await PayPalProcessor.GetPayPalToken(_configuration);
        //        var canceledSubscription = await PayPalProcessor.CancelSubscription(requestToken, subscriptionInDb.Id);

        //        if (canceledSubscription)
        //        {
        //            // Get the subscription so we can get the new state
        //            var subscriptionJson = await PayPalProcessor.GetSubscription(requestToken, subscriptionInDb.Id);
        //            var subscriptionObject = JObject.Parse(subscriptionJson);

        //            subscriptionInDb.Status = (string)subscriptionObject["status"];

        //            _context.SaveChanges();

        //            return Json(new { success = true });
        //        }
        //        else
        //        {
        //            return Json(new { success = false });
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        //    }
        //}

        [HttpGet]
        [ActionName("SubscriptionCancel")]
        public IActionResult SubscriptionCancelGet()
        {
            return View("SubscriptionCancel");
        }

        //
        //GET: /Account/SubscriptionCancel
        [HttpPost]
        public IActionResult SubscriptionCancel()
        {
            try
            {
                string currentUserId = User.GetUserId();

                var userInDb = _context.Users.Where(u => u.Id == currentUserId).FirstOrDefault();

                var subscriptionInDb = _context.StripeSubscriptions.Where(s => s.Status.ToLower() == "active" && s.CustomerId == _context.StripeCustomers.Where(c => c.UserId == currentUserId).Select(c => c.Id).FirstOrDefault()).FirstOrDefault();

                if (subscriptionInDb == null)
                {
                    subscriptionInDb = _context.StripeSubscriptions.Where(s => s.Status.ToLower() == "trialing" && s.CustomerId == _context.StripeCustomers.Where(c => c.UserId == currentUserId).Select(c => c.Id).FirstOrDefault()).FirstOrDefault();

                    if (subscriptionInDb == null)
                    {
                        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                    }                        
                }

                // Get the subscription so we can get the new state
                var subscription = StripeProcessor.CancelSubscription(_configuration, subscriptionInDb);

                subscriptionInDb.Status = subscription.Status;

                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception e)
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        //
        // GET: /Account/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {            
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }
        }

        //
        // GET: /Account/RenewSubscription
        [HttpGet]
        public IActionResult RenewSubscription()
        {
            return View("CreditCard");
        }

        //
        // GET: /Account/AddSubscription
        [HttpGet]
        public IActionResult AddSubscription()
        {        
            return View("CreditCard");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ProcessStripe(StripePaymentViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    string currentUserId = model.UserId == null ? User.GetUserId() : model.UserId;

                    var userInDb = _context.Users.Where(u => u.Id == currentUserId).FirstOrDefault();

                    var productInDb = _context.StripeProducts.Where(p => p.Name == "TabCollab").FirstOrDefault();

                    // No product so we need to create one
                    if (productInDb == null)
                    {
                        var product = StripeProcessor.CreateProduct(_configuration);

                        productInDb = new StripeProduct
                        {
                            Id = product.Id,
                            Name = product.Name
                        };

                        _context.StripeProducts.Add(productInDb);
                        _context.SaveChanges();
                    }

                    var planInDb = _context.StripePlans.Where(p => p.Nickname == "TabCollab Standard Subscription").FirstOrDefault();

                    // No plan so we need to create one
                    if (planInDb == null)
                    {
                        var plan = StripeProcessor.CreatePlan(_configuration, productInDb);

                        planInDb = new StripePlan
                        {
                            Id = plan.Id,
                            Nickname = plan.Nickname,
                            ProductId = productInDb.Id
                        };

                        _context.StripePlans.Add(planInDb);
                        _context.SaveChanges();
                    }

                    var customerInDb = _context.StripeCustomers.Where(c => c.UserId == currentUserId).FirstOrDefault();

                    // No customer so we need to create one
                    if (customerInDb == null)
                    {
                        var customer = StripeProcessor.CreateCustomer(_configuration, userInDb, model.PaymentToken);

                        customerInDb = new StripeCustomer
                        {
                            Id = customer.Id,
                            UserId = currentUserId,
                            SubscriptionId = null
                        };

                        userInDb.CustomerId = customer.Id;

                        _context.StripeCustomers.Add(customerInDb);
                        _context.SaveChanges();
                    }

                    var subscriptionInDb = _context.StripeSubscriptions.Where(s => s.CustomerId == customerInDb.Id && (s.Status.ToLower() == "active" || s.Status.ToLower() == "trialing")).FirstOrDefault();

                    // No subscription so we need to create one
                    if (subscriptionInDb == null)
                    {
                        bool updateSubscriptionExpiration = true;
                        Stripe.Subscription subscription = null;

                        if (userInDb.SubscriptionExpiration != null)
                        {
                            // If we have a current subscription expriation and it has expired, just start a brand new subscription
                            if ((int)(userInDb.SubscriptionExpiration - DateTime.Now).Value.TotalDays < 0)
                            {
                                subscription = StripeProcessor.CreateSubscription(_configuration, planInDb, customerInDb, userInDb, false);
                            }
                            // Otherwise, we need to make sure we start from the end of our current subscription
                            else
                            {
                                subscription = StripeProcessor.CreateSubscription(_configuration, planInDb, customerInDb, userInDb, true);
                                updateSubscriptionExpiration = false;
                            }
                        }
                        // We've never had a subscription, so start a new one
                        else
                        {
                            subscription = StripeProcessor.CreateSubscription(_configuration, planInDb, customerInDb, userInDb, false);
                        }
                        

                        subscriptionInDb = new StripeSubscription
                        {
                            Id = subscription.Id,
                            CustomerId = customerInDb.Id,
                            PlanId = planInDb.Id,
                            Status = subscription.Status
                        };

                        customerInDb.SubscriptionId = subscription.Id;

                        if (subscriptionInDb.Status.ToLower() == "active" || subscriptionInDb.Status.ToLower() == "trialing")
                        {
                            userInDb.AccountType = AccountType.Subscription;

                            if (updateSubscriptionExpiration)
                            {
                                userInDb.SubscriptionExpiration = subscription.CurrentPeriodEnd;
                            }                                

                            _context.StripeSubscriptions.Add(subscriptionInDb);
                            _context.SaveChanges();

                            return PartialView("_SubscriptionConfirmation");
                        }
                        else
                        {
                            return PartialView("_SubscriptionFailure");
                        }
                    }
                    else
                    {
                        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                    }
                }

                var modelErrors = ModelState.Values.SelectMany(v => v.Errors.Select(b => b.ErrorMessage)).ToList();

                return Json(new { error = string.Join("<br />", modelErrors) });
            }
            catch (Stripe.StripeException e)
            {
                return Json(new { param = e.StripeError.Parameter, error = e.StripeError.Message });
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            try
            {
                ViewData["ReturnUrl"] = returnUrl;
                if (ModelState.IsValid)
                {
                    // Default to Free account type and change if we get a successful credit card charge
                    var user = new ApplicationUser
                    {
                        UserName = model.Username,
                        Email = model.Email,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Bio = "",
                        AccountType = AccountType.Free,
                        SubscriptionExpiration = null
                    };
                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        string partialView = "_RegisterConfirmation";

                        if (model.AccountType == AccountType.Subscription)
                        {
                            partialView = "_CreditCardForm";
                            ViewBag.UserId = user.Id;
                            ViewBag.FromRegistration = true;
                        }

                        // Save profile image if it was added
                        if (model.Image != null)
                        {
                            // Limit file size to 1 MB
                            if (model.Image.Length <= 1000000)
                            {
                                string currentUserId = user.Id;

                                var userInDb = _context.Users.SingleOrDefault(u => u.Id == currentUserId);

                                string imageFilePath = await _fileUploader.UploadFileToFileSystem(model.Image, currentUserId, "Profile");

                                userInDb.ImageFileName = model.Image.FileName;
                                userInDb.ImageFilePath = imageFilePath;

                                _context.SaveChanges();
                            }
                        }

                        // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=532713
                        // Send an email with this link
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        var callbackUrl = Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
                        await _emailSender.SendEmailAsync(model.Email, "Confirm your account",
                            $"Please confirm your account by clicking this link: <a href='{callbackUrl}'>link</a>");

                        _logger.LogInformation(3, "User created a new account with password.");
                        return PartialView(partialView);
                    }

                    // If we got here there were errors in user creation
                    var errors = string.Join("<br />", result.Errors.Select(e => e.Description).ToList());

                    return Json(new { error = errors });
                }

                // If we got here there were errors in the modelstate                
                var modelErrors = ModelState.Values.SelectMany(v => v.Errors.Select(b => b.ErrorMessage)).ToList();

                return Json(new { error = string.Join("<br />", modelErrors) });
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }

        //
        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation(4, "User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        //
        // GET: /Account/ExternalLoginCallback
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
                return View(nameof(Login));
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            if (result.Succeeded)
            {
                _logger.LogInformation(5, "User logged in with {Name} provider.", info.LoginProvider);
                return RedirectToLocal(returnUrl);
            }
            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl });
            }
            if (result.IsLockedOut)
            {
                return View("Lockout");
            }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["LoginProvider"] = info.LoginProvider;
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Username, Email = model.Email, FirstName = model.FirstName, LastName = model.LastName };
                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    // Save profile image if it was added
                    if (model.Image != null)
                    {
                        // Limit file size to 1 MB
                        if (model.Image.Length > 1000000)
                        {
                            return StatusCode(StatusCodes.Status500InternalServerError, "Image size limit is 1 MB");
                        }

                        string currentUserId = user.Id;

                        var userInDb = _context.Users.SingleOrDefault(u => u.Id == currentUserId);

                        string imageFilePath = await _fileUploader.UploadFileToFileSystem(model.Image, currentUserId, "Profile");

                        userInDb.ImageFileName = model.Image.FileName;
                        userInDb.ImageFilePath = imageFilePath;

                        _context.SaveChanges();
                    }

                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        _logger.LogInformation(6, "User created an account using {Name} provider.", info.LoginProvider);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        // GET: /Account/ConfirmEmail
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return View("Error");
            }
            var result = await _userManager.ConfirmEmailAsync(user, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // GET: /Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userByUsername = await _userManager.FindByNameAsync(model.Username);

                if (userByUsername == null || !(await _userManager.IsEmailConfirmedAsync(userByUsername)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return PartialView("_ForgotPasswordConfirmation");
                }

                if (userByUsername != null)
                {
                    if (userByUsername.Email != model.Email)
                    {
                        // Don't reveal that the user does not exist or is not confirmed
                        return PartialView("_ForgotPasswordConfirmation");
                    }
                }

                // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=532713
                // Send an email with this link
                var code = await _userManager.GeneratePasswordResetTokenAsync(userByUsername);
                var callbackUrl = Url.Action(nameof(ResetPassword), "Account", new { userId = userByUsername.Id, code = code }, protocol: HttpContext.Request.Scheme);
                await _emailSender.SendEmailAsync(model.Email, "Reset Password",
                   $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");
                return PartialView("_ForgotPasswordConfirmation");
            }

            // If we got this far, something failed
            return PartialView("_ForgotPasswordForm", model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var userByUserName = await _userManager.FindByNameAsync(model.Username);
            if (userByUserName == null)
            {
                // Don't reveal that the user does not exist
                return PartialView("_ResetPasswordConfirmation");
            }
            else if (userByUserName.Email != model.Email)
            {
                // Don't reveal that the user does not exist
                return PartialView("_ResetPasswordConfirmation");
            }
            var result = await _userManager.ResetPasswordAsync(userByUserName, model.Code, model.Password);
            if (result.Succeeded)
            {
                return PartialView("_ResetPasswordConfirmation");
            }
            AddErrors(result);

            return PartialView("_ResetPasswordForm", model);
        }


        //
        // GET: /Account/SendCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl = null, bool rememberMe = false)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            var userFactors = await _userManager.GetValidTwoFactorProvidersAsync(user);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }

            // Generate the token and send it
            var code = await _userManager.GenerateTwoFactorTokenAsync(user, model.SelectedProvider);
            if (string.IsNullOrWhiteSpace(code))
            {
                return View("Error");
            }

            var message = "Your security code is: " + code;
            if (model.SelectedProvider == "Email")
            {
                await _emailSender.SendEmailAsync(await _userManager.GetEmailAsync(user), "Security Code", message);
            }
            else if (model.SelectedProvider == "Phone")
            {
                await _smsSender.SendSmsAsync(await _userManager.GetPhoneNumberAsync(user), message);
            }

            return RedirectToAction(nameof(VerifyCode), new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        //
        // GET: /Account/VerifyCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyCode(string provider, bool rememberMe, string returnUrl = null)
        {
            // Require that the user has already logged in via username/password or external login
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes.
            // If a user enters incorrect codes for a specified amount of time then the user account
            // will be locked out for a specified amount of time.
            var result = await _signInManager.TwoFactorSignInAsync(model.Provider, model.Code, model.RememberMe, model.RememberBrowser);
            if (result.Succeeded)
            {
                return RedirectToLocal(model.ReturnUrl);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning(7, "User account locked out.");
                return View("Lockout");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid code.");
                return View(model);
            }
        }

        //
        // GET /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        #endregion
    }
}
