using System.Linq;
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
using Microsoft.EntityFrameworkCore;
using Stripe;
using TabRepository.Models.ManageViewModels;
using static TabRepository.Controllers.ManageController;
using Avalara.AvaTax.RestClient;

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
        private ApplicationDbContext _context;
        private FileUploader _fileUploader;
        private readonly IHostingEnvironment _appEnvironment;
        private IConfiguration _configuration;
        private StripeProcessor _stripeProcessor;
        private UserAuthenticator _userAuthenticator;
        private readonly List<string> _taxableStates = new List<string> { "texas", "tx" };

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ISmsSender smsSender,
            ILoggerFactory loggerFactory,
            ApplicationDbContext context,
            IHostingEnvironment appEnvironment,
            IConfiguration configuration,
            UserAuthenticator userAuthenticator)
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
            _stripeProcessor = new StripeProcessor(_appEnvironment, _configuration);
            _userAuthenticator = userAuthenticator;
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
                if (!GoogleReCaptchaProcessor.ReCaptchaPassed(_configuration, model.ReCaptchaToken))
                {
                    return Json(new { error = "You failed the ReCaptcha. If you are not a robot, please try again." });
                }

                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    var userInDb = _context.Users
                        .Where(u => u.UserName.ToLower() == model.Username.ToLower())
                        .FirstOrDefault();

                    var subscriptionInDb = _context.StripeSubscriptions
                        .Where(s => (s.Status.ToLower() == "active" || s.Status.ToLower() == "past_due" || s.Status.ToLower() == "trialing") 
                        && s.CustomerId == _context.StripeCustomers
                            .Where(c => c.UserId == userInDb.Id)
                            .Select(c => c.Id)
                            .FirstOrDefault())
                        .FirstOrDefault();

                    Models.AccountViewModels.AccountType prevAccountType = userInDb.AccountType;

                    if (!userInDb.AccountTypeLocked)
                    {
                        if (subscriptionInDb != null)
                        {
                            var subscription = _stripeProcessor.GetSubscription(_configuration, subscriptionInDb);

                            subscriptionInDb.Status = subscription.Status;
                            subscriptionInDb.CancelAtPeriodEnd = subscription.CancelAtPeriodEnd;

                            // If the subscription is no longer active, change to free account
                            if (subscription.Status.ToLower() == "active" || subscription.Status.ToLower() == "trialing" || subscription.Status.ToLower() == "past_due")
                            {
                                userInDb.AccountType = Models.AccountViewModels.AccountType.Pro;
                            }
                            else
                            {
                                userInDb.AccountType = Models.AccountViewModels.AccountType.Free;
                            }

                            _context.SaveChanges();
                        }
                        else
                        {
                            userInDb.AccountType = Models.AccountViewModels.AccountType.Free;
                            _context.SaveChanges();
                        }

                        userInDb.LastLogin = DateTime.Now;
                        _context.SaveChanges();

                        if (prevAccountType != userInDb.AccountType)
                        {
                            NotificationsController.AddNotification(
                                _context,
                                NotificationType.AccountTypeChanged,
                                userInDb,
                                null,
                                null,
                                userInDb.AccountType.ToString(),
                                null
                            );
                        }
                    }

                    _logger.LogInformation(1, "User logged in.");
                    return Json(new { url = returnUrl });
                }
                //if (result.RequiresTwoFactor)
                //{
                //    return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                //}
                //if (result.IsLockedOut)
                //{
                //    _logger.LogWarning(2, "User account locked out.");
                //    return View("Lockout");
                //}
                //else
                //{
                //    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                //    return View(model);
                //}

                return Json(new { error = "Invalid login attempt" });
            }

            if (model.ReCaptchaToken == null)
            {
                return Json(new { error = "You failed the ReCaptcha. If you are not a robot, please try again." });
            }

            // If we got here there were errors in the modelstate                
            var modelErrors = ModelState.Values.SelectMany(v => v.Errors.Select(b => b.ErrorMessage)).ToList();

            return Json(new { error = string.Join("<br />", modelErrors) });
        }

        //#region Subscriptions

        //[HttpGet]
        //[ActionName("SubscriptionCancel")]
        //public IActionResult SubscriptionCancelGet()
        //{
        //    return View("SubscriptionCancel");
        //}

        ////
        ////GET: /Account/SubscriptionCancel
        //[HttpPost]
        //public IActionResult SubscriptionCancel()
        //{
        //    try
        //    {
        //        string currentUserId = User.GetUserId();

        //        var userInDb = _context.Users.Where(u => u.Id == currentUserId).FirstOrDefault();

        //        var subscriptionInDb = _context.StripeSubscriptions.Where(s => (s.Status.ToLower() == "active" || s.Status.ToLower() == "past_due" || s.Status.ToLower() == "trialing") && s.CustomerId == _context.StripeCustomers.Where(c => c.UserId == currentUserId).Select(c => c.Id).FirstOrDefault()).FirstOrDefault();

        //        if (subscriptionInDb == null)
        //        {
        //            return Json(new { error = "Active subscription does not exist" });
        //        }

        //        // Get the subscription so we can get the new state
        //        var subscription = _stripeProcessor.CancelSubscription(_configuration, subscriptionInDb);

        //        subscriptionInDb.Status = subscription.Status;
        //        subscriptionInDb.CancelAtPeriodEnd = subscription.CancelAtPeriodEnd;

        //        // If the subscription is no longer active, set to free account
        //        if (subscription.Status.ToLower() != "active" && subscription.Status.ToLower() != "past_due" && subscription.Status.ToLower() != "trialing")
        //        {
        //            userInDb.AccountType = Models.AccountViewModels.AccountType.Free;
        //        }

        //        _context.SaveChanges();

        //        return Json(new { success = true });
        //    }
        //    catch (Exception e)
        //    {
        //        return Json(new { error = e.Message });
        //    }
        //}

        //[HttpPost]
        //public IActionResult SubscriptionReactivate()
        //{
        //    try
        //    {
        //        string currentUserId = User.GetUserId();

        //        var userInDb = _context.Users.Where(u => u.Id == currentUserId).FirstOrDefault();

        //        var subscriptionInDb = _context.StripeSubscriptions.Where(s => (s.Status.ToLower() == "active" || s.Status.ToLower() == "past_due" || s.Status.ToLower() == "trialing") && s.CustomerId == _context.StripeCustomers.Where(c => c.UserId == currentUserId).Select(c => c.Id).FirstOrDefault()).FirstOrDefault();

        //        if (subscriptionInDb == null)
        //        {
        //            return Json(new { error = "Active subscription does not exist" });
        //        }

        //        var subscription = _stripeProcessor.ActivateSubscription(_configuration, subscriptionInDb);

        //        subscriptionInDb.Status = subscription.Status;
        //        subscriptionInDb.CancelAtPeriodEnd = subscription.CancelAtPeriodEnd;

        //        _context.SaveChanges();

        //        if (subscriptionInDb.Status.ToLower() == "active" || subscriptionInDb.Status.ToLower() == "past_due" || subscriptionInDb.Status.ToLower() == "trialing")
        //        {
        //            return Json(new { success = true });
        //        }
        //        else
        //        {
        //            return Json(new { error = "Active subscription does not exist" });
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        return Json(new { error = e.Message });
        //    }
        //}


        //// GET: /Account/Subscribe
        //[HttpGet]
        //public IActionResult Subscribe()
        //{
        //    string currentUserId = User.GetUserId();

        //    var userInDb = _context.Users.Where(u => u.Id == currentUserId).FirstOrDefault();

        //    var subscriptionInDb = _context.StripeSubscriptions.Where(s => (s.Status.ToLower() == "active" || s.Status.ToLower() == "past_due" || s.Status.ToLower() == "trialing") && s.Customer.UserId == currentUserId).FirstOrDefault();

        //    if (subscriptionInDb == null)
        //    {
        //        if (_appEnvironment.IsDevelopment())
        //        {
        //            ViewData["StripeKey"] = _configuration["Stripe:TestPublishable"];
        //        }
        //        else
        //        {
        //            ViewData["StripeKey"] = _configuration["Stripe:LivePublishable"];
        //        }

        //        return View("CreditCard");
        //    }
        //    else
        //    {
        //        return RedirectToAction("Index", "Home");
        //    }
        //}


        //// GET: /Account/UpdatePayment
        //[HttpGet]
        //public IActionResult UpdatePayment()
        //{
        //    string currentUserId = User.GetUserId();

        //    var userInDb = _context.Users.Where(u => u.Id == currentUserId).FirstOrDefault();

        //    var subscriptionInDb = _context.StripeSubscriptions.Where(s => (s.Status.ToLower() == "active" || s.Status.ToLower() == "past_due" || s.Status.ToLower() == "trialing") && s.Customer.UserId == currentUserId).FirstOrDefault();

        //    if (subscriptionInDb != null)
        //    {
        //        return View("CreditCardUpdate");
        //    }
        //    else
        //    {
        //        return RedirectToAction("Index", "Home");
        //    }
        //}


        //// GET: /Account/UpdatePayment
        //[HttpPost]
        //public IActionResult UpdatePayment(StripePaymentViewModel model)
        //{
        //    try
        //    {
        //        if (ModelState.IsValid)
        //        {
        //            string currentUserId = model.UserId == null ? User.GetUserId() : model.UserId;

        //            var userInDb = _context.Users.Where(u => u.Id == currentUserId).FirstOrDefault();

        //            var customerInDb = _context.StripeCustomers.Where(c => c.UserId == currentUserId).FirstOrDefault();

        //            // Update customer's payment
        //            if (customerInDb != null)
        //            {
        //                var customer = _stripeProcessor.UpdateCustomerPayment(_configuration, customerInDb, model.PaymentToken);

        //                var subscriptionInDb = _context.StripeSubscriptions.Where(s => s.CustomerId == customerInDb.Id && (s.Status.ToLower() == "active" || s.Status.ToLower() == "past_due" || s.Status.ToLower() == "trialing")).FirstOrDefault();

        //                // Update the tax rate for the subscription, if we have one
        //                if (subscriptionInDb != null)
        //                {
        //                    StripeTaxRate taxRateInDb = null;

        //                    var taxRate = createTaxRate(model.StripeAddress, model.StripeCity, model.StripeState, model.StripeZip, currentUserId);

        //                    if (taxRate != null)
        //                    {
        //                        taxRateInDb = new StripeTaxRate
        //                        {
        //                            Id = taxRate.Id,
        //                            SubscriptionId = subscriptionInDb.Id,
        //                            Percentage = taxRate.Percentage,
        //                            StripeDescription = taxRate.Description,
        //                            StripeJurisdicion = taxRate.Jurisdiction,
        //                            State = model.StripeState,
        //                            Zip = model.StripeZip
        //                        };
        //                    }

        //                    var subscription = _stripeProcessor.UpdateSubscription(_configuration, subscriptionInDb.Id, taxRate);

        //                    subscriptionInDb.Status = subscription.Status;

        //                    if (taxRateInDb != null)
        //                    {
        //                        _context.StripeTaxRates.Add(taxRateInDb);
        //                    }

        //                    _context.SaveChanges();
        //                }

        //                return PartialView("_UpdateCreditCardConfirmation");
        //            }
        //            else
        //            {
        //                return PartialView("_UpdateCreditCardFailure");
        //            }
        //        }

        //        var modelErrors = ModelState.Values.SelectMany(v => v.Errors.Select(b => b.ErrorMessage)).ToList();

        //        return Json(new { error = string.Join("<br />", modelErrors) });
        //    }
        //    catch (Stripe.StripeException e)
        //    {
        //        return Json(new { param = e.StripeError.Parameter, error = e.StripeError.Message });
        //    }
        //    catch (Exception e)
        //    {
        //        return Json(new { error = e.Message });
        //    }
        //}

        //[HttpPost]
        //[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        //public IActionResult SubscriptionProcess(StripePaymentViewModel model)
        //{
        //    try
        //    {
        //        if (ModelState.IsValid)
        //        {
        //            string currentUserId = model.UserId == null ? User.GetUserId() : model.UserId;

        //            var userInDb = _context.Users.Where(u => u.Id == currentUserId).FirstOrDefault();

        //            var productInDb = _context.StripeProducts.Where(p => p.Name == "TabCollab").FirstOrDefault();

        //            // No product so we need to create one
        //            if (productInDb == null)
        //            {
        //                var product = _stripeProcessor.CreateProduct(_configuration);

        //                productInDb = new StripeProduct
        //                {
        //                    Id = product.Id,
        //                    Name = product.Name
        //                };

        //                _context.StripeProducts.Add(productInDb);
        //                _context.SaveChanges();
        //            }

        //            // Hardcoded to Pro for now until Composers tier is added
        //            var planInDb = _context.StripePlans.Where(p => p.Nickname == $"TabCollab Pro {model.StripeInterval.ToString()} Subscription").FirstOrDefault();

        //            // No plan so we need to create one
        //            if (planInDb == null)
        //            {
        //                var plan = _stripeProcessor.CreatePlan(_configuration, productInDb, model.StripeInterval);

        //                planInDb = new StripePlan
        //                {
        //                    Id = plan.Id,
        //                    Nickname = plan.Nickname,
        //                    ProductId = productInDb.Id,
        //                    Interval = plan.Interval
        //                };

        //                _context.StripePlans.Add(planInDb);
        //                _context.SaveChanges();
        //            }

        //            var customerInDb = _context.StripeCustomers.Where(c => c.UserId == currentUserId).FirstOrDefault();

        //            // No customer so we need to create one
        //            if (customerInDb == null)
        //            {
        //                var customer = _stripeProcessor.CreateCustomer(_configuration, userInDb, model.PaymentToken);

        //                customerInDb = new StripeCustomer
        //                {
        //                    Id = customer.Id,
        //                    UserId = currentUserId
        //                };

        //                _context.StripeCustomers.Add(customerInDb);
        //                _context.SaveChanges();
        //            }
        //            // We already have a customer, so update their payment info. This can happen if payment fails initially
        //            else
        //            {
        //                var customer = _stripeProcessor.UpdateCustomerPayment(_configuration, customerInDb, model.PaymentToken);
        //            }

        //            var subscriptionInDb = _context.StripeSubscriptions.Where(s => s.CustomerId == customerInDb.Id && (s.Status.ToLower() == "active" || s.Status.ToLower() == "past_due" || s.Status.ToLower() == "trialing")).FirstOrDefault();

        //            // No active subscription so we need to create one
        //            if (subscriptionInDb == null)
        //            {
        //                Stripe.Subscription subscription = null;
        //                StripeTaxRate taxRateInDb = null;

        //                var taxRate = createTaxRate(model.StripeAddress, model.StripeCity, model.StripeState, model.StripeZip, currentUserId);

        //                if (taxRate != null)
        //                {
        //                    taxRateInDb = new StripeTaxRate
        //                    {
        //                        Id = taxRate.Id,
        //                        Percentage = taxRate.Percentage,
        //                        StripeDescription = taxRate.Description,
        //                        StripeJurisdicion = taxRate.Jurisdiction,
        //                        State = model.StripeState,
        //                        Zip = model.StripeZip
        //                    };
        //                }

        //                subscription = _stripeProcessor.CreateSubscription(_configuration, planInDb, customerInDb, userInDb, taxRate);

        //                subscriptionInDb = new StripeSubscription
        //                {
        //                    Id = subscription.Id,
        //                    PlanId = planInDb.Id,
        //                    Status = subscription.Status,
        //                    CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
        //                    CustomerId = customerInDb.Id
        //                };

        //                if (taxRateInDb != null)
        //                {
        //                    taxRateInDb.SubscriptionId = subscriptionInDb.Id;
        //                    _context.StripeTaxRates.Add(taxRateInDb);
        //                }

        //                customerInDb.Id = subscription.CustomerId;

        //                _context.StripeSubscriptions.Add(subscriptionInDb);

        //                _context.SaveChanges();

        //                Models.AccountViewModels.AccountType prevAccountType = userInDb.AccountType;

        //                if (subscriptionInDb.Status.ToLower() == "active" || subscriptionInDb.Status.ToLower() == "trialing" || subscriptionInDb.Status.ToLower() == "past_due")
        //                {
        //                    userInDb.AccountType = Models.AccountViewModels.AccountType.Pro;

        //                    _context.SaveChanges();

        //                    if (prevAccountType != userInDb.AccountType)
        //                    {
        //                        NotificationsController.AddNotification(
        //                            _context,
        //                            NotificationType.AccountTypeChanged,
        //                            userInDb,
        //                            null,
        //                            null,
        //                            userInDb.AccountType.ToString(),
        //                            null
        //                        );
        //                    }

        //                    return PartialView("_SubscriptionConfirmation");
        //                }
        //                else
        //                {
        //                    return PartialView("_SubscriptionFailure");
        //                }
        //            }
        //            // If we have an active subscription, we shouldn't be here
        //            else
        //            {
        //                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        //            }
        //        }

        //        var modelErrors = ModelState.Values.SelectMany(v => v.Errors.Select(b => b.ErrorMessage)).ToList();

        //        return Json(new { error = string.Join("<br />", modelErrors) });
        //    }
        //    catch (Stripe.StripeException e)
        //    {
        //        return Json(new { param = e.StripeError.Parameter, error = e.StripeError.Message });
        //    }
        //    catch (Exception e)
        //    {
        //        return Json(new { error = e.Message });
        //    }
        //}

        //private TaxRate createTaxRate(string address, string city, string state, string zip, string userId)
        //{
        //    var currentUserId = userId;
        //    var userInDb = _context.Users.Where(u => u.Id == currentUserId).FirstOrDefault();
        //    List<string> descriptions = new List<string>();

        //    // Create a client and set up authentication
        //    var client = new AvaTaxClient("TabCollab", "1.0", Environment.MachineName, AvaTaxEnvironment.Production)
        //        .WithSecurity(_configuration["AvalaraTax:Username"], _configuration["AvalaraTax:Password"]);

        //    var rate = client.TaxRatesByAddress(address, null, null, city, state, zip, "us");

        //    // If this is not a taxable state, create a tax rate of 0.00% so we can still track revenue by state
        //    if (!_taxableStates.Contains(state.ToLower()) || rate.rates.Count == 0)
        //    {
        //        descriptions = rate.rates.Select(r => r.name).ToList();

        //        return _stripeProcessor.CreateTaxRate(_configuration, userInDb, String.Join(" | ", descriptions), state.ToUpper() + " - " + city.ToUpper(), 0.00m);
        //    }

        //    descriptions = rate.rates.Select(r => r.name).ToList();

        //    return _stripeProcessor.CreateTaxRate(_configuration, userInDb, String.Join(" | ", descriptions), state.ToUpper() + " - " + city.ToUpper(), rate.totalRate * 100);
        //}

        //[HttpGet]
        //[AllowAnonymous]
        //public IActionResult CalculateTaxRate(string address, string city, string state, string zip)
        //{
        //    try
        //    {
        //        // Create a client and set up authentication
        //        var client = new AvaTaxClient("TabCollab", "1.0", Environment.MachineName, AvaTaxEnvironment.Production)
        //            .WithSecurity(_configuration["AvalaraTax:Username"], _configuration["AvalaraTax:Password"]);

        //        var rate = client.TaxRatesByAddress(address, null, null, city, state, zip, "us");

        //        if (!_taxableStates.Contains(state.ToLower()) || rate.rates.Count == 0)
        //        {
        //            return Json(new { taxRate = 0.00m, });
        //        }

        //        return Json(new { taxRate = rate.totalRate });
        //    }
        //    catch (AvaTaxError e)
        //    {
        //        return Json(new { error = e.error.error.message });
        //    }
        //    catch (Exception e)
        //    {
        //        return Json(new { error = e.Message });
        //    }
        //}

        //#endregion

        [HttpGet]
        public IActionResult Billing()
        {
            try
            {
                string currentUserId = User.GetUserId();
                List<BillingViewModel> viewModel = new List<BillingViewModel>();

                var invoices = _context.StripeInvoices                    
                    .Include(i => i.Customer)
                    .Include(i => i.Subscription)
                    .ThenInclude(s => s.Plan)
                    .Where(i => i.Customer.UserId == currentUserId)
                    .OrderByDescending(i => i.DateCreated)
                    .ToList();

                foreach (StripeInvoice invoice in invoices)
                {
                    BillingViewModel vm = new BillingViewModel()
                    {
                        Subtotal = invoice.Subtotal,
                        Tax = invoice.Tax,
                        ReceiptURL = invoice.ReceiptURL,
                        DateCreated = invoice.DateCreated,
                        DateDue = invoice.DateDue,
                        DatePaid = invoice.DatePaid,
                        PaymentStatus = invoice.PaymentStatus,
                        PaymentStatusText = invoice.PaymentStatusText,
                        Interval = invoice.Subscription == null ? SubscriptionInterval.None : (invoice.Subscription.Plan.Interval.ToLower() == "month" ? SubscriptionInterval.Monthly : SubscriptionInterval.Yearly)
                    };

                    viewModel.Add(vm);
                }

                return View(viewModel);
            }
            catch (Exception e)
            {
                return RedirectToAction("Error", "Home", new { code = 500 });
            }
        }

        //
        // GET: /Account/Index
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
                    return RedirectToAction("Error", "Home", new { code = 500 });
                }

                // Get a count of total tab versions that this user owns (i.e. their projects)
                var tabCount = _context.Tabs.Include(u => u.User)
                    .Include(t => t.Album)
                    .Include(t => t.Album.Project)
                    .Where(t => t.Album.Project.UserId == currentUserId)
                    .Count();

                var tabVersionCount = _context.TabVersions.Include(u => u.User)
                    .Include(v => v.Tab)
                    .Include(v => v.Tab.Album)
                    .Include(v => v.Tab.Album.Project)
                    .Where(v => v.Tab.Album.Project.UserId == currentUserId)
                    .Count();

                SubscriptionStatus subscriptionStatus = SubscriptionStatus.None;
                string creditCardLastFour = null;
                string creditCardExpiration = null;
                StripeSubscription subscriptionInDb = null;

                var customerInDb = _context.StripeCustomers.Where(c => c.UserId == currentUserId).FirstOrDefault();

                if (customerInDb != null)
                {
                    subscriptionInDb = _context.StripeSubscriptions
                        .Include(s => s.Plan)
                        .Include(s => s.Customer)
                        .Where(s => (s.Status.ToLower() == "active" || s.Status.ToLower() == "past_due" || s.Status.ToLower() == "trialing") && s.CustomerId == customerInDb.Id)
                        .FirstOrDefault();
                }

                if (subscriptionInDb != null)
                {
                    if (subscriptionInDb.Status.ToLower() == "past_due")
                    {
                        subscriptionStatus = SubscriptionStatus.PastDue;
                    }
                    else
                    {
                        subscriptionStatus = SubscriptionStatus.Active;
                    }
                }

                if (customerInDb != null)
                {
                    var customer = _stripeProcessor.GetCustomer(_configuration, customerInDb);

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
                    ImageFileName = user.ImageFileName,
                    ImageFilePath = user.ImageFilePath,
                    TabCount = tabCount,
                    TabVersionCount = tabVersionCount,
                    Email = user.Email,
                    SubscriptionStatus = subscriptionStatus,
                    AccountType = user.AccountType,
                    CreditCardExpiration = creditCardExpiration,
                    CreditCardLast4 = creditCardLastFour,
                    Interval = subscriptionInDb != null ? subscriptionInDb.Plan.Interval : "",
                    CancelAtPeriodEnd = subscriptionInDb != null ? subscriptionInDb.CancelAtPeriodEnd : default(bool)
                };
                return View(model);
            }
            catch (Exception e)
            {
                return RedirectToAction("Error", "Home", new { code = 500 });
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

                if (_appEnvironment.IsDevelopment())
                {
                    ViewData["StripeKey"] = _configuration["Stripe:TestPublishable"];
                }
                else
                {
                    ViewData["StripeKey"] = _configuration["Stripe:LivePublishable"];
                }

                return View();
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
                    if (!GoogleReCaptchaProcessor.ReCaptchaPassed(_configuration, model.ReCaptchaToken))
                    {
                        return Json(new { error = "You failed the ReCaptcha. If you are not a robot, please try again." });
                    }

                    // Default to Free account type and change if we get a successful credit card charge
                    var user = new ApplicationUser
                    {
                        UserName = model.Username,
                        Email = model.Email,
                        Bio = "",
                        AccountType = Models.AccountViewModels.AccountType.Free
                    };

                    var emailInDb = _context.Users.Where(u => u.Email == model.Email).FirstOrDefault();

                    if (emailInDb != null)
                    {
                        return Json(new { error = "Email is already in use." });
                    }

                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        string partialView = "_RegisterConfirmation";
                        
                        if (model.AccountType == Models.AccountViewModels.AccountType.Pro)
                        {
                            partialView = "_CreditCardForm";
                            ViewBag.UserId = user.Id;
                            ViewBag.FromRegistration = true;
                        }

                        // Save profile image if it was added
                        if (model.CroppedImage != null)
                        {
                            // Limit file size to 1 MB
                            if (model.CroppedImage.Length <= 1000000)
                            {
                                string currentUserId = user.Id;

                                var userInDb = _context.Users.SingleOrDefault(u => u.Id == currentUserId);

                                Helpers.File file = await _fileUploader.UploadFileToFileSystem(model.CroppedImage, currentUserId, "Profile");

                                userInDb.ImageFileName = file.Name;
                                userInDb.ImageFilePath = file.Path;

                                _context.SaveChanges();
                            }
                        }

                        // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=532713
                        // Send an email with this link
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        var callbackUrl = Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
                        await _emailSender.SendEmailAsync(
                            _configuration,
                            model.Email, 
                            "Confirm your account",
                            String.Format(@"
                                Hi {0}!

                                Thanks for creating your TabCollab account.To start using your account, please verify your email by clicking  <a href='{1}'>here</a>.", model.Username, callbackUrl),
                            HtmlTemplate.GetConfirmEmailHtml(model.Username, callbackUrl)
                        );

                        _logger.LogInformation(3, "User created a new account with password.");
                        return PartialView(partialView);
                    }

                    // If we got here there were errors in user creation
                    var errors = string.Join("<br />", result.Errors.Select(e => e.Description).ToList());

                    return Json(new { error = errors });
                }

                if (model.ReCaptchaToken == null)
                {
                    return Json(new { error = "You failed the ReCaptcha. If you are not a robot, please try again." });
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

        [HttpPost]        
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendEmail(string username, string subject, string message)
        {
            try
            {
                string currentUserId = User.GetUserId();

                var currentUserInDb = _context.Users.Where(u => u.Id == currentUserId).FirstOrDefault();

                if (currentUserInDb == null || currentUserInDb.UserName.ToLower() != "tolleway")
                {
                    return Json(new { error = "Unable to send email." });
                }

                var toUserInDb = _context.Users.Where(u => u.UserName.ToLower() == username).FirstOrDefault();

                if (toUserInDb == null)
                {
                    return Json(new { error = "Unable to send email." });
                }

                await _emailSender.SendEmailAsync(
                    _configuration,
                    toUserInDb.Email,
                    subject,
                    String.Format(@"
                        Hi {0}!

                        {1}", toUserInDb.UserName, message),
                    HtmlTemplate.GetDynamicEmailHtml(toUserInDb.UserName, message)
                );

                return Json(new { email = toUserInDb.Email });
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

                        Helpers.File file = await _fileUploader.UploadFileToFileSystem(model.Image, currentUserId, "Profile");

                        userInDb.ImageFileName = file.Name;
                        userInDb.ImageFilePath = file.Path;

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
                await _emailSender.SendEmailAsync(_configuration, model.Email, "Reset Password",
                   $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>",
                   $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");
                return PartialView("_ForgotPasswordConfirmation");
            }
            
            return Json(new { error = "An error occurred. Please try again." });
        }

        //
        // GET: /Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResendConfirmationEmail()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendConfirmationEmail(ResendConfirmationEmailViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var userByUsername = await _userManager.FindByNameAsync(model.Username);

                    if (userByUsername == null)
                    {
                        // Don't reveal that the user does not exist
                        return PartialView("_ResendConfirmationEmailConfirmation");
                    }

                    if (userByUsername.Email != model.Email)
                    {
                        // Don't reveal that the email does not match
                        return PartialView("_ResendConfirmationEmailConfirmation");
                    }

                    // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=532713
                    // Send an email with this link
                    if (!userByUsername.EmailConfirmed)
                    {
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(userByUsername);
                        var callbackUrl = Url.Action(nameof(ConfirmEmail), "Account", new { userId = userByUsername.Id, code = code }, protocol: HttpContext.Request.Scheme);
                        await _emailSender.SendEmailAsync(
                            _configuration,
                            model.Email,
                            "Confirm your account",
                            String.Format(@"
                                Hi {0}!

                                Thanks for creating your TabCollab account.To start using your account, please verify your email by clicking  <a href='{1}'>here</a>.", model.Username, callbackUrl),
                            HtmlTemplate.GetConfirmEmailHtml(model.Username, callbackUrl)
                        );
                    }
                    return PartialView("_ResendConfirmationEmailConfirmation");
                }

                // If we got this far, something failed
                return Json(new { error = "An error has occurred. Please try again." });
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
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
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                var userByUserName = await _userManager.FindByNameAsync(model.Username);
                if (userByUserName == null)
                {
                    return Json(new { error = "Username not found" });
                }

                var result = await _userManager.ResetPasswordAsync(userByUserName, model.Code, model.Password);
                if (result.Succeeded)
                {
                    return PartialView("_ResetPasswordConfirmation");
                }

                // If we got here there were errors in user creation
                var errors = string.Join("<br />", result.Errors.Select(e => e.Description).ToList());

                return Json(new { error = errors });
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
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
                await _emailSender.SendEmailAsync(_configuration, await _userManager.GetEmailAsync(user), "Security Code", message, message);
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

        private Task<ApplicationUser> GetCurrentUserAsync()
        {
            return _userManager.GetUserAsync(HttpContext.User);
        }

        #endregion
    }
}
