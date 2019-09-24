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

                    if (invoiceInDb == null)
                    {
                        invoiceInDb = CreateInvoice(stripeEvent.Data.Object as Invoice);

                        _context.StripeInvoices.Add(invoiceInDb);
                        _context.SaveChanges();
                    }
                }
            }

            if (stripeEvent.Type == "invoice.payment_succeeded")
            {
                lock (_newInvoiceLock)
                {
                    var invoiceInDb = _context.StripeInvoices.Where(i => i.Id == invoiceId).FirstOrDefault();

                    if (invoiceInDb == null)
                    {
                        invoiceInDb = CreateInvoice(stripeEvent.Data.Object as Invoice);

                        _context.StripeInvoices.Add(invoiceInDb);
                        _context.SaveChanges();
                    }

                    if (subscriptionInDb.Status.ToLower() == "trialing")
                    {
                        invoiceInDb.ChargeId = "";
                        invoiceInDb.ReceiptURL = "";
                        invoiceInDb.DatePaid = DateTime.Now;
                        invoiceInDb.PaymentStatus = PaymentStatus.Unpaid;
                        invoiceInDb.PaymentStatusText = "Trialing";

                        _context.SaveChanges();
                    }
                    else
                    {
                        Charge charge = _stripeProcessor.GetCharge(_configuration, (stripeEvent.Data.Object as Invoice).ChargeId);

                        invoiceInDb.ChargeId = charge.Id;
                        invoiceInDb.ReceiptURL = charge.ReceiptUrl;
                        invoiceInDb.DatePaid = DateTime.Now;
                        invoiceInDb.PaymentStatus = PaymentStatus.Paid;
                        invoiceInDb.PaymentStatusText = charge.Outcome.SellerMessage;

                        _context.SaveChanges();
                    }
                }
            }
            else if (stripeEvent.Type == "invoice.payment_failed")
            {
                lock (_newInvoiceLock)
                {
                    Charge charge = _stripeProcessor.GetCharge(_configuration, (stripeEvent.Data.Object as Invoice).ChargeId);

                    var invoiceInDb = _context.StripeInvoices.Where(i => i.Id == invoiceId).FirstOrDefault();

                    if (invoiceInDb == null)
                    {
                        invoiceInDb = CreateInvoice(stripeEvent.Data.Object as Invoice);

                        _context.StripeInvoices.Add(invoiceInDb);
                        _context.SaveChanges();
                    }

                    invoiceInDb.PaymentStatus = PaymentStatus.Failed;
                    invoiceInDb.PaymentStatusText = charge.FailureMessage;

                    _context.SaveChanges();
                }
            }

            // We need to make sure that the subscription for this message is still active
            if (subscriptionInDb != null)
            {
                var userInDb = _context.Users.Where(u => u.Id == subscriptionInDb.Customer.UserId).FirstOrDefault();
                var subscription = _stripeProcessor.GetSubscription(_configuration, subscriptionInDb);
                subscriptionInDb.Status = subscription.Status.ToLower();

                if (subscription.Status.ToLower() == "active" || subscription.Status.ToLower() == "trialing")
                {
                    userInDb.AccountType = Models.AccountViewModels.AccountType.Pro;
                }
                else
                {
                    userInDb.AccountType = Models.AccountViewModels.AccountType.Free;
                }

                _context.SaveChanges();
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
                PaymentStatus = PaymentStatus.Unpaid            
            };

            return stripeInvoice;
        }
    }
}