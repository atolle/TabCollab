using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;
using TabRepository.Data;
using TabRepository.Helpers;
using TabRepository.Models;

namespace TabRepository.Controllers.Api
{
    [Produces("application/json")]
    [Route("api/StripeWebhook")]
    public class StripeWebhookController : Controller
    {
        private ApplicationDbContext _context;
        private IConfiguration _configuration;
        private IHostingEnvironment _env;
        private StripeProcessor _stripeProcessor;
        private static readonly object _newInvoiceLock = new object();

        public StripeWebhookController(ApplicationDbContext context, IConfiguration configuration, IHostingEnvironment env)
        {
            _context = context;
            _configuration = configuration;
            _env = env;
            _stripeProcessor = new StripeProcessor(_env, _configuration);
        }

        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
        }

        [HttpPost]
        public IActionResult Index()
        {
            var json = new StreamReader(HttpContext.Request.Body).ReadToEnd();
            var stripeEvent = EventUtility.ParseEvent(json);

            string subscriptionId = "";
            string invoiceId = "";           

            switch (stripeEvent.Type)
            {
                case "customer.subscription.updated":
                case "customer.subscription.deleted":
                    subscriptionId = (stripeEvent.Data.Object as Subscription).Id;
                    break;
                case "invoice.payment_succeeded":
                case "invoice.payment_failed":
                case "invoice.finalized":
                case "invoice.created":
                case "invoice.updated":
                    subscriptionId = (stripeEvent.Data.Object as Invoice).SubscriptionId;
                    invoiceId = (stripeEvent.Data.Object as Invoice).Id;
                    break;
            }

            var subscriptionInDb = _context.StripeSubscriptions.Include(s => s.Customer).Where(s => s.Id == subscriptionId).FirstOrDefault();            
            
            if (stripeEvent.Type == "invoice.created")
            {
                lock (_newInvoiceLock)
                {
                    var invoiceInDb = _context.StripeInvoices.Where(i => i.Id == invoiceId).FirstOrDefault();
                    var userInDb = _context.Users.Where(u => u.Id == _context.StripeCustomers.Where(c => c.Id == (stripeEvent.Data.Object as Invoice).CustomerId).Select(c => c.UserId).FirstOrDefault()).FirstOrDefault();

                    if (invoiceInDb == null)
                    {                        
                        invoiceInDb = CreateInvoice(stripeEvent.Data.Object as Invoice);

                        _context.StripeInvoices.Add(invoiceInDb);
                        _context.SaveChanges();

                        NotificationsController.AddNotification(
                            _context, 
                            NotificationType.InvoiceCreated, 
                            userInDb, 
                            null, 
                            null, 
                            null, 
                            null
                        );
                    }
                }
            }

            if (stripeEvent.Type == "invoice.updated")
            {
                lock (_newInvoiceLock)
                {
                    var invoiceInDb = _context.StripeInvoices.Where(i => i.Id == invoiceId).FirstOrDefault();
                    var userInDb = _context.Users.Where(u => u.Id == _context.StripeCustomers.Where(c => c.Id == (stripeEvent.Data.Object as Invoice).CustomerId).Select(c => c.UserId).FirstOrDefault()).FirstOrDefault();

                    if (invoiceInDb == null)
                    {
                        invoiceInDb = CreateInvoice(stripeEvent.Data.Object as Invoice);

                        _context.StripeInvoices.Add(invoiceInDb);
                        _context.SaveChanges();
                    }
                    
                    invoiceInDb.DateDue = (stripeEvent.Data.Object as Invoice).DueDate;
                    invoiceInDb.Subtotal = (stripeEvent.Data.Object as Invoice).Subtotal;
                    invoiceInDb.Tax = (stripeEvent.Data.Object as Invoice).Tax ?? default(double);

                    _context.SaveChanges();

                    NotificationsController.AddNotification(
                        _context, 
                        NotificationType.InvoiceUpdated, 
                        userInDb, 
                        null, 
                        null, 
                        null, 
                        null
                    );
                }
            }

            if (stripeEvent.Type == "invoice.payment_succeeded")
            {
                lock (_newInvoiceLock)
                {
                    var invoiceInDb = _context.StripeInvoices.Where(i => i.Id == invoiceId).FirstOrDefault();
                    var userInDb = _context.Users.Where(u => u.Id == _context.StripeCustomers.Where(c => c.Id == (stripeEvent.Data.Object as Invoice).CustomerId).Select(c => c.UserId).FirstOrDefault()).FirstOrDefault();
                    bool addNotification = false;

                    if (invoiceInDb == null)
                    {
                        invoiceInDb = CreateInvoice(stripeEvent.Data.Object as Invoice);

                        _context.StripeInvoices.Add(invoiceInDb);
                        _context.SaveChanges();

                        addNotification = true;
                    }

                    if (subscriptionInDb != null && subscriptionInDb.Status.ToLower() == "trialing")
                    {
                        invoiceInDb.ChargeId = "";
                        invoiceInDb.ReceiptURL = "";
                        invoiceInDb.DatePaid = DateTime.Now;
                        invoiceInDb.PaymentStatus = PaymentStatus.Unpaid;
                        invoiceInDb.PaymentStatusText = "Trialing";

                        _context.SaveChanges();

                        addNotification = true;
                    }
                    else
                    {
                        Charge charge = _stripeProcessor.GetCharge(_configuration, (stripeEvent.Data.Object as Invoice).ChargeId);

                        invoiceInDb.TaxRateId = (stripeEvent.Data.Object as Invoice).DefaultTaxRates[0].Id;
                        invoiceInDb.ChargeId = charge.Id;
                        invoiceInDb.ReceiptURL = charge.ReceiptUrl;
                        invoiceInDb.DatePaid = DateTime.Now;
                        invoiceInDb.PaymentStatus = PaymentStatus.Paid;
                        invoiceInDb.PaymentStatusText = charge.Outcome.SellerMessage;

                        _context.SaveChanges();

                        addNotification = true;
                    }

                    if (addNotification)
                    {
                        NotificationsController.AddNotification(
                            _context, 
                            NotificationType.InvoicePaid, 
                            userInDb, 
                            null, 
                            null, 
                            null, 
                            null
                        );
                    }
                }
            }

            if (stripeEvent.Type == "invoice.payment_failed")
            {
                lock (_newInvoiceLock)
                {
                    Charge charge = _stripeProcessor.GetCharge(_configuration, (stripeEvent.Data.Object as Invoice).ChargeId);
                    var invoiceInDb = _context.StripeInvoices.Where(i => i.Id == invoiceId).FirstOrDefault();
                    var userInDb = _context.Users.Where(u => u.Id == _context.StripeCustomers.Where(c => c.Id == (stripeEvent.Data.Object as Invoice).CustomerId).Select(c => c.UserId).FirstOrDefault()).FirstOrDefault();

                    if (invoiceInDb == null)
                    {
                        invoiceInDb = CreateInvoice(stripeEvent.Data.Object as Invoice);

                        _context.StripeInvoices.Add(invoiceInDb);
                        _context.SaveChanges();
                    }

                    invoiceInDb.PaymentStatus = PaymentStatus.Failed;
                    invoiceInDb.PaymentStatusText = charge.FailureMessage;

                    _context.SaveChanges();

                    NotificationsController.AddNotification(
                        _context, 
                        NotificationType.InvoicePaymentFailed, 
                        userInDb, 
                        null, 
                        null, 
                        null, 
                        null
                    );
                }
            }

            // We need to make sure that the subscription for this message is still active
            if (subscriptionInDb != null)
            {
                var userInDb = _context.Users.Where(u => u.Id == subscriptionInDb.Customer.UserId).FirstOrDefault();
                var subscription = _stripeProcessor.GetSubscription(_configuration, subscriptionInDb);
                Models.AccountViewModels.AccountType prevAccountType = userInDb.AccountType;
                string prevSubscriptionStatus = subscriptionInDb.Status;

                subscriptionInDb.Status = subscription.Status.ToLower();

                // Subscriptions need to be active, trialing, or past due, in which case we will retry payment and then subscription status becomes canceled
                if (subscription.Status.ToLower() == "active" || subscription.Status.ToLower() == "trialing" || subscription.Status.ToLower() == "past_due")
                { 
                    userInDb.AccountType = Models.AccountViewModels.AccountType.Pro;
                }
                else
                {
                    userInDb.AccountType = Models.AccountViewModels.AccountType.Free;
                }

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

                if (prevSubscriptionStatus.ToLower() != subscriptionInDb.Status.ToLower())
                {
                    NotificationsController.AddNotification(
                        _context, 
                        NotificationType.SubscriptionStatusUpdated, 
                        userInDb, 
                        null, 
                        null, 
                        subscriptionInDb.Status, 
                        null
                    );
                }
            }

            return new StatusCodeResult(StatusCodes.Status200OK);
        }

        private StripeInvoice CreateInvoice(Invoice invoice)
        {
            StripeInvoice stripeInvoice = new StripeInvoice
            {
                Id = invoice.Id,
                SubscriptionId = invoice.SubscriptionId,
                Subtotal = invoice.Subtotal,
                Tax = invoice.Tax ?? default(double),
                DateCreated = DateTime.Now,
                DateDue = invoice.DueDate ?? DateTime.Now,
                PaymentStatus = PaymentStatus.Unpaid,
                CustomerId = invoice.CustomerId
            };            

            return stripeInvoice;
        }
    }
}